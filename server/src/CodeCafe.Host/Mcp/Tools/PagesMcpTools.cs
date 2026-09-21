using System.ComponentModel;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Pages.CreatePage;
using CodeCafe.Application.Pages.DeletePage;
using CodeCafe.Application.Pages.GetPage;
using CodeCafe.Application.Pages.GetPageByPath;
using CodeCafe.Application.Pages.MovePage;
using CodeCafe.Application.Pages.SetPageFavorite;
using CodeCafe.Application.Pages.SharePage;
using CodeCafe.Application.Pages.UpdatePage;
using CodeCafe.Domain.Sharing;
using MediatR;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CodeCafe.Host.Mcp.Tools;

[McpServerToolType]
public sealed class PagesMcpTools
{
    [McpServerTool(Name = "codecafe_get_page", ReadOnly = true, Idempotent = true)]
    [Description("Get a page with all its blocks.")]
    public static async Task<CallToolResult> GetPage(
        ISender sender,
        Guid pageId,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new GetPageQuery(pageId), cancellationToken));

    [McpServerTool(Name = "codecafe_get_page_by_path", ReadOnly = true, Idempotent = true)]
    [Description("Get a page by its path inside a notebook, e.g. /guides/setup.")]
    public static async Task<CallToolResult> GetPageByPath(
        ISender sender,
        string notebookIdOrSlug,
        string path,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new GetPageByPathQuery(notebookIdOrSlug, path), cancellationToken));

    [McpServerTool(Name = "codecafe_create_page")]
    [Description("Create a page in a notebook, optionally with initial blocks.")]
    public static async Task<CallToolResult> CreatePage(
        ISender sender,
        string notebookIdOrSlug,
        string title,
        CancellationToken cancellationToken,
        string? parentPath = null,
        IReadOnlyList<BlockInput>? blocks = null,
        BlockContentFormat? format = null)
        => McpToolResults.From(await sender.Send(new CreatePageCommand(notebookIdOrSlug, title, parentPath, blocks, format), cancellationToken));

    [McpServerTool(Name = "codecafe_update_page", Idempotent = true)]
    [Description("Rename a page and/or change its archived state.")]
    public static async Task<CallToolResult> UpdatePage(
        ISender sender,
        Guid pageId,
        CancellationToken cancellationToken,
        string? title = null,
        bool? isArchived = null)
        => McpToolResults.From(await sender.Send(new UpdatePageCommand(pageId, title, isArchived), cancellationToken));

    [McpServerTool(Name = "codecafe_move_page", Idempotent = true)]
    [Description("Move a page to another parent path or reorder it after a sibling page.")]
    public static async Task<CallToolResult> MovePage(
        ISender sender,
        Guid pageId,
        CancellationToken cancellationToken,
        string? parentPath = null,
        Guid? afterPageId = null)
        => McpToolResults.From(await sender.Send(new MovePageCommand(pageId, parentPath, afterPageId), cancellationToken));

    [McpServerTool(Name = "codecafe_set_page_favorite", Idempotent = true)]
    [Description("Mark or unmark a page as favorite.")]
    public static async Task<CallToolResult> SetPageFavorite(
        ISender sender,
        Guid pageId,
        bool isFavorite,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new SetPageFavoriteCommand(pageId, isFavorite), cancellationToken));

    [McpServerTool(Name = "codecafe_share_page", Idempotent = true)]
    [Description("Share a single page with another user by email.")]
    public static async Task<CallToolResult> SharePage(
        ISender sender,
        Guid pageId,
        string email,
        CollaboratorRole role,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new SharePageCommand(pageId, email, role), cancellationToken));

    [McpServerTool(Name = "codecafe_revoke_page_share", Idempotent = true, Destructive = true)]
    [Description("Revoke a user's access to a shared page.")]
    public static async Task<CallToolResult> RevokePageShare(
        ISender sender,
        Guid pageId,
        Guid userId,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new RevokePageShareCommand(pageId, userId), cancellationToken));

    [McpServerTool(Name = "codecafe_delete_page", Destructive = true, Idempotent = true)]
    [Description("Permanently delete a page. This cannot be undone; archive it instead if unsure.")]
    public static async Task<CallToolResult> DeletePage(
        ISender sender,
        Guid pageId,
        CancellationToken cancellationToken)
        => McpToolResults.From(await sender.Send(new DeletePageCommand(pageId), cancellationToken));
}
