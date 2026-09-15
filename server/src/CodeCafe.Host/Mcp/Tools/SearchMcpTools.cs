using System.ComponentModel;
using CodeCafe.Application.Search.Queries;
using MediatR;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeCafe.Host.Mcp.Tools;

[McpServerToolType]
public sealed class SearchMcpTools
{
    [McpServerTool(Name = "codecafe_search", ReadOnly = true, Idempotent = true)]
    [Description("Search pages across all notebooks visible to the current user.")]
    public static async Task<CallToolResult> Search(
        ISender sender,
        string query,
        CancellationToken cancellationToken,
        string? cursor = null,
        int? pageSize = null)
        => McpToolResults.From(await sender.Send(new SearchAllPagesQuery(query, cursor, pageSize), cancellationToken));
}
