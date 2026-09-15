using System.ComponentModel;
using CodeCafe.Application.Trash.Commands;
using CodeCafe.Application.Trash.Queries;
using MediatR;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeCafe.Host.Mcp.Tools;

[McpServerToolType]
public sealed class TrashMcpTools
{
    [McpServerTool(Name = "codecafe_list_trash", ReadOnly = true, Idempotent = true)]
    [Description("List notebooks currently in the trash.")]
    public static async Task<CallToolResult> ListTrash(
        ISender sender,
        CancellationToken cancellationToken,
        string? cursor = null,
        int? pageSize = null)
        => McpToolResults.From(await sender.Send(new ListTrashQuery(cursor, pageSize), cancellationToken));

    [McpServerTool(Name = "codecafe_restore_notebook", Idempotent = true)]
    [Description("Restore a notebook from the trash.")]
    public static async Task<CallToolResult> RestoreNotebook(
        ISender sender,
        Guid notebookId,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new RestoreNotebookFromTrashCommand(notebookId), cancellationToken));

    [McpServerTool(Name = "codecafe_purge_notebook", Destructive = true, Idempotent = true)]
    [Description("Permanently delete one trashed notebook. This cannot be undone.")]
    public static async Task<CallToolResult> PurgeNotebook(
        ISender sender,
        Guid notebookId,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new PurgeNotebookCommand(notebookId), cancellationToken));

    [McpServerTool(Name = "codecafe_empty_trash", Destructive = true)]
    [Description("Permanently delete everything in the trash. This cannot be undone.")]
    public static async Task<CallToolResult> EmptyTrash(
        ISender sender,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new EmptyTrashCommand(), cancellationToken));
}
