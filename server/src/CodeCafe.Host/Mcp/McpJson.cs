using System.Text.Encodings.Web;
using CodeCafe.Application.Notes;
using System.Text.Json;
using System.Text.Unicode;

namespace CodeCafe.Host.Mcp;

internal static class McpJson
{
    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    internal static JsonElement SerializeToElement<T>(T value)
        => JsonSerializer.SerializeToElement(value, SerializerOptions);

    internal static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, SerializerOptions);
}
