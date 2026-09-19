using System.ComponentModel;
using CodeCafe.Application.Common;
using CodeCafe.Application.Notebooks.ChangeNotebookSlug;
using CodeCafe.Application.Notebooks.CreateNotebook;
using CodeCafe.Application.Notebooks.DeleteNotebook;
using CodeCafe.Application.Notebooks.ImportNotebook;
using CodeCafe.Application.Notebooks.RevokeNotebookShare;
using CodeCafe.Application.Notebooks.SetNotebookAccessCode;
using CodeCafe.Application.Notebooks.SetNotebookFavorite;
using CodeCafe.Application.Notebooks.SetNotebookTags;
using CodeCafe.Application.Notebooks.ShareNotebook;
using CodeCafe.Application.Notebooks.UpdateNotebook;
using CodeCafe.Application.Notebooks.ExportNotebook;
using CodeCafe.Application.Notebooks.Shared;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
using CodeCafe.Application.Notebooks.GetNotebookTree;
using CodeCafe.Application.Notebooks.ListNotebooks;
using CodeCafe.Application.Notebooks.SearchNotebookPages;
using MediatR;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeCafe.Host.Mcp.Tools;

[McpServerToolType]
public sealed class NotebooksMcpTools
{
    [McpServerTool(Name = "codecafe_list_notebooks", ReadOnly = true, Idempotent = true)]
    [Description("List notebooks visible to the current user, with optional filters and cursor pagination.")]
    public static async Task<CallToolResult> ListNotebooks(
        ISender sender,
        CancellationToken cancellationToken,
        string? tag = null,
        bool? favorite = null,
        NotebookVisibility? visibility = null,
        string? cursor = null,
        int? pageSize = null)
        => McpToolResults.From(await sender.Send(new ListNotebooksQuery(tag, favorite, visibility, cursor, pageSize), cancellationToken));

    [McpServerTool(Name = "codecafe_get_notebook", ReadOnly = true, Idempotent = true)]
    [Description("Get notebook details by id or slug.")]
    public static async Task<CallToolResult> GetNotebook(
        ISender sender,
        string idOrSlug,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new GetNotebookDetailsQuery(idOrSlug), cancellationToken));

    [McpServerTool(Name = "codecafe_get_notebook_tree", ReadOnly = true, Idempotent = true)]
    [Description("Get the page tree of a notebook by id or slug.")]
    public static async Task<CallToolResult> GetNotebookTree(
        ISender sender,
        string idOrSlug,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new GetNotebookTreeQuery(idOrSlug), cancellationToken));

    [McpServerTool(Name = "codecafe_export_notebook", ReadOnly = true, Idempotent = true)]
    [Description("Export a whole notebook as markdown.")]
    public static async Task<CallToolResult> ExportNotebook(
        ISender sender,
        string idOrSlug,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new ExportNotebookQuery(idOrSlug), cancellationToken));

    [McpServerTool(Name = "codecafe_search_notebook", ReadOnly = true, Idempotent = true)]
    [Description("Search pages inside one notebook.")]
    public static async Task<CallToolResult> SearchNotebook(
        ISender sender,
        string idOrSlug,
        string query,
        CancellationToken cancellationToken,
        string? cursor = null,
        int? pageSize = null)
        => McpToolResults.From(await sender.Send(new SearchNotebookPagesQuery(idOrSlug, query, cursor, pageSize), cancellationToken));

    [McpServerTool(Name = "codecafe_create_notebook")]
    [Description("Create a new notebook.")]
    public static async Task<CallToolResult> CreateNotebook(
        ISender sender,
        string title,
        CancellationToken cancellationToken,
        string? description = null,
        string? slug = null,
        NotebookVisibility visibility = NotebookVisibility.Private)
        => McpToolResults.From(await sender.Send(new CreateNotebookCommand(title, description, slug, visibility), cancellationToken));

    [McpServerTool(Name = "codecafe_update_notebook")]
    [Description("Update a notebook's title, description, or visibility.")]
    public static async Task<CallToolResult> UpdateNotebook(
        ISender sender,
        string idOrSlug,
        CancellationToken cancellationToken,
        string? title = null,
        string? description = null,
        NotebookVisibility? visibility = null)
        => McpToolResults.From(await sender.Send(new UpdateNotebookCommand(idOrSlug, title, description, visibility), cancellationToken));

    [McpServerTool(Name = "codecafe_import_notebook")]
    [Description("Import a notebook from a previously exported markdown payload.")]
    public static async Task<CallToolResult> ImportNotebook(
        ISender sender,
        string fileName,
        string markdown,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new ImportNotebookCommand(new NotebookExportDto(fileName, markdown)), cancellationToken));

    [McpServerTool(Name = "codecafe_change_notebook_slug", Idempotent = true)]
    [Description("Change a notebook's URL slug.")]
    public static async Task<CallToolResult> ChangeNotebookSlug(
        ISender sender,
        string idOrSlug,
        string slug,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new ChangeNotebookSlugCommand(idOrSlug, slug), cancellationToken));

    [McpServerTool(Name = "codecafe_set_notebook_access_code", Idempotent = true)]
    [Description("Set or clear the access code guarding a notebook.")]
    public static async Task<CallToolResult> SetNotebookAccessCode(
        ISender sender,
        string idOrSlug,
        CancellationToken cancellationToken,
        string? accessCode = null)
        => McpToolResults.From(await sender.Send(new SetNotebookAccessCodeCommand(idOrSlug, accessCode), cancellationToken));

    [McpServerTool(Name = "codecafe_set_notebook_favorite", Idempotent = true)]
    [Description("Mark or unmark a notebook as favorite.")]
    public static async Task<CallToolResult> SetNotebookFavorite(
        ISender sender,
        string idOrSlug,
        bool isFavorite,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new SetNotebookFavoriteCommand(idOrSlug, isFavorite), cancellationToken));

    [McpServerTool(Name = "codecafe_set_notebook_tags", Idempotent = true)]
    [Description("Replace all tags of a notebook.")]
    public static async Task<CallToolResult> SetNotebookTags(
        ISender sender,
        string idOrSlug,
        IReadOnlyList<string> tags,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new SetNotebookTagsCommand(idOrSlug, tags), cancellationToken));

    [McpServerTool(Name = "codecafe_share_notebook", Idempotent = true)]
    [Description("Share a notebook with another user by email.")]
    public static async Task<CallToolResult> ShareNotebook(
        ISender sender,
        string idOrSlug,
        string email,
        CollaboratorRole role,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new ShareNotebookCommand(idOrSlug, email, role), cancellationToken));

    [McpServerTool(Name = "codecafe_revoke_notebook_share", Idempotent = true, Destructive = true)]
    [Description("Revoke a user's access to a shared notebook.")]
    public static async Task<CallToolResult> RevokeNotebookShare(
        ISender sender,
        string idOrSlug,
        Guid userId,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new RevokeNotebookShareCommand(idOrSlug, userId), cancellationToken));

    [McpServerTool(Name = "codecafe_delete_notebook", Destructive = true, Idempotent = true)]
    [Description("Move a notebook to the trash. It can be restored from there.")]
    public static async Task<CallToolResult> DeleteNotebook(
        ISender sender,
        string idOrSlug,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new DeleteNotebookCommand(idOrSlug), cancellationToken));
}
