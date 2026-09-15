using System.ComponentModel;
using CodeCafe.Application.Revisions.Commands;
using CodeCafe.Application.Revisions.Queries;
using MediatR;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeCafe.Host.Mcp.Tools;

[McpServerToolType]
public sealed class RevisionsMcpTools
{
    [McpServerTool(Name = "codecafe_list_page_revisions", ReadOnly = true, Idempotent = true)]
    [Description("List the revision history of a page, newest first.")]
    public static async Task<CallToolResult> ListPageRevisions(
        ISender sender,
        Guid pageId,
        CancellationToken cancellationToken,
        string? cursor = null,
        int? pageSize = null)
        => McpToolResults.From(await sender.Send(new ListPageRevisionsQuery(pageId, cursor, pageSize), cancellationToken));

    [McpServerTool(Name = "codecafe_list_block_revisions", ReadOnly = true, Idempotent = true)]
    [Description("List the revision history of one block.")]
    public static async Task<CallToolResult> ListBlockRevisions(
        ISender sender,
        Guid pageId,
        Guid blockId,
        CancellationToken cancellationToken,
        string? cursor = null,
        int? pageSize = null)
        => McpToolResults.From(await sender.Send(new ListBlockRevisionsQuery(pageId, blockId, cursor, pageSize), cancellationToken));

    [McpServerTool(Name = "codecafe_restore_page_revision", Destructive = true, Idempotent = true)]
    [Description("Roll a whole page back to its state at the given point in time.")]
    public static async Task<CallToolResult> RestorePageRevision(
        ISender sender,
        Guid pageId,
        DateTimeOffset atUtc,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new RestorePageToRevisionCommand(pageId, atUtc), cancellationToken));

    [McpServerTool(Name = "codecafe_restore_block_revision", Destructive = true, Idempotent = true)]
    [Description("Roll one block back to a specific revision number.")]
    public static async Task<CallToolResult> RestoreBlockRevision(
        ISender sender,
        Guid pageId,
        Guid blockId,
        long revision,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new RestoreBlockRevisionCommand(pageId, blockId, revision), cancellationToken));
}
