using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCafe.Application.Common;
using CodeCafe.Application.Notebooks.Queries;
using CodeCafe.Application.Pages.Queries;
using MediatR;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeCafe.Host.Mcp.Resources;

// Read-only views of notebooks and pages, for MCP clients that prefer browsing resources
// over calling tools. Failures surface as JSON-RPC errors via McpException.
[McpServerResourceType]
public sealed class NotebookMcpResources
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [McpServerResource(UriTemplate = "codecafe://notebooks", Name = "notebooks", Title = "Notebooks", MimeType = "application/json")]
    [Description("All notebooks visible to the current user.")]
    public static async Task<ReadResourceResult> ListNotebooks(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListNotebooksQuery(null, null, null, null, null), cancellationToken);
        return ToResourceResult(result, "codecafe://notebooks");
    }

    [McpServerResource(UriTemplate = "codecafe://pages/{pageId}", Name = "page", Title = "Page", MimeType = "application/json")]
    [Description("A page with all its blocks.")]
    public static async Task<ReadResourceResult> GetPage(ISender sender, Guid pageId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPageQuery(pageId), cancellationToken);
        return ToResourceResult(result, $"codecafe://pages/{pageId}");
    }

    private static ReadResourceResult ToResourceResult<T>(Result<T> result, string uri)
    {
        if (!result.IsSuccess)
        {
            var error = result.Error!;
            throw new McpException($"{error.Kind}: {error.Code}: {error.Message}");
        }

        return new ReadResourceResult
        {
            Contents =
            [
                new TextResourceContents
                {
                    Uri = uri,
                    MimeType = "application/json",
                    Text = JsonSerializer.Serialize(result.Value, JsonOptions)
                }
            ]
        };
    }
}
