using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCafe.Application.Common;
using ModelContextProtocol.Protocol;
using Result = CodeCafe.Application.Common.Result;

namespace CodeCafe.Host.Mcp;

// Translates the application Result into MCP-native tool semantics: failures become
// CallToolResult.IsError (so the model sees a failed call), successes become structured
// content plus a JSON text block for clients that ignore structured content.
internal static class McpToolResults
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static CallToolResult From<T>(Result<T> result)
        => result.IsSuccess ? Success(result.Value) : Failure(result.Error!);

    public static CallToolResult From(Result result)
        => result.IsSuccess
            ? new CallToolResult { Content = [new TextContentBlock { Text = "Done." }] }
            : Failure(result.Error!);

    public static CallToolResult Success<T>(T? value)
    {
        if (value is null)
            return new CallToolResult { Content = [new TextContentBlock { Text = "Done." }] };

        var json = JsonSerializer.Serialize(value, JsonOptions);
        var result = new CallToolResult { Content = [new TextContentBlock { Text = json }] };

        var structured = JsonSerializer.SerializeToElement(value, JsonOptions);
        // MCP requires structuredContent to be a JSON object; wrap arrays and scalars.
        result.StructuredContent = structured.ValueKind == JsonValueKind.Object
            ? structured
            : JsonSerializer.SerializeToElement(new Dictionary<string, JsonElement> { ["value"] = structured }, JsonOptions);
        return result;
    }

    private static CallToolResult Failure(Error error) => new()
    {
        IsError = true,
        Content = [new TextContentBlock { Text = $"{error.Kind}: {error.Code}: {error.Message}" }]
    };
}
