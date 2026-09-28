using System.ComponentModel;
using CodeCafe.Application.Trash.PurgeTrashedNotebooks;
using CodeCafe.Application.Trash.ListTrash;
using CodeCafe.Application.Trash.ListTrashedPages;
using CodeCafe.Application.Trash.PurgeNotebook;
using CodeCafe.Application.Trash.PurgePage;
using CodeCafe.Application.Trash.RestoreNotebookFromTrash;
using CodeCafe.Application.Trash.RestorePageFromTrash;
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
        int? page = null,
        int? pageSize = null)
        => McpToolResults.From(await sender.Send(new ListTrashQuery(page, pageSize), cancellationToken));

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

    [McpServerTool(Name = "codecafe_purge_trashed_notebooks", Destructive = true, Idempotent = true)]
    [Description("Permanently delete every trashed notebook and all its contents. Pages trashed individually inside a live notebook are not affected. This cannot be undone.")]
    public static async Task<CallToolResult> PurgeTrashedNotebooks(
        ISender sender,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new PurgeTrashedNotebooksCommand(), cancellationToken));

    [McpServerTool(Name = "codecafe_list_trashed_pages", ReadOnly = true, Idempotent = true)]
    [Description("List the pages currently in a notebook's trash, by notebook id or slug.")]
    public static async Task<CallToolResult> ListTrashedPages(
        ISender sender,
        string idOrSlug,
        CancellationToken cancellationToken,
        int? page = null,
        int? pageSize = null)
        => McpToolResults.From(await sender.Send(new ListTrashedPagesQuery(idOrSlug, page, pageSize), cancellationToken));

    [McpServerTool(Name = "codecafe_restore_page", Idempotent = true)]
    [Description("Restore a page and its subtree from the trash.")]
    public static async Task<CallToolResult> RestorePage(
        ISender sender,
        Guid pageId,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new RestorePageFromTrashCommand(pageId), cancellationToken));

    [McpServerTool(Name = "codecafe_purge_page", Destructive = true, Idempotent = true)]
    [Description("Permanently delete one trashed page and its subtree. This cannot be undone.")]
    public static async Task<CallToolResult> PurgePage(
        ISender sender,
        Guid pageId,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new PurgePageCommand(pageId), cancellationToken));
}
