using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Shared.Application.Configuration;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Claims;

namespace CodeCafe.Modules.Mcp.Tools.Notes;

public sealed partial class NotesMcpItemTools
{
    [McpServerTool(
        Name = NotesMcpToolNames.ListItems,
        Title = "List Notebook Items",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(ListNotebookItemsToolResponse))]
    [Description("List folder and page metadata visible to the authenticated actor, with optional parent, type, archive, and pagination filters. Page bodies are omitted; use notes_get_page to read full content.")]
    public async Task<CallToolResult> ListItemsAsync(
        [Description("The notebook slug.")] string notebookSlug,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        CancellationToken cancellationToken,
        IOptions<McpOptions> mcpOptionsAccessor,
        [Description("Optional search term to filter notebook items.")] string? search = null,
        [Description("Optional parent folder path. When provided, only direct children of that folder are returned. " + PathCompatibilityDescription)] string? parentPath = null,
        [Description("Filter item type: all, page, or folder.")] string? type = null,
        [Description("Include archived items in the result set. Only the notebook owner can use this.")] bool includeArchived = false,
        [Description("Zero-based offset for pagination.")] int? offset = null,
        [Description("Maximum number of items to return.")] int? limit = null)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var notebookContextResult = await NotesMcpSupport.RequireNotebookSummaryContextAsync(
            notebookSlug,
            user,
            notebookReadService,
            cancellationToken,
            mcpOptions.RequiredReadScopes,
            includeArchived);
        if (!notebookContextResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookContextResult.Error!);
        }

        var notebookContext = notebookContextResult.Value;
        var notebook = notebookContext.Notebook;
        if (includeArchived && notebook.OwnerId != notebookContext.ActorId)
        {
            return NotesMcpResultMapper.Failure(new NotesError(
                NotesFailureKind.Forbidden,
                "notebook_forbidden",
                "Only the notebook owner can view archived items."));
        }

        var normalizedType = string.IsNullOrWhiteSpace(type) ? "all" : type.Trim().ToLowerInvariant();
        if (normalizedType is not ("all" or "page" or "folder"))
        {
            return NotesMcpResultMapper.Failure(new NotesError(
                NotesFailureKind.Validation,
                "invalid_type",
                "Type must be all, page, or folder."));
        }

        Guid? parentIdFilter = null;
        if (!string.IsNullOrWhiteSpace(parentPath))
        {
            var parentResult = await NotesMcpSupport.ResolveParentAsync(
                notebook,
                parentPath,
                notebookContext.ActorId,
                notebookReadService,
                cancellationToken,
                includeArchived);
            if (!parentResult.Succeeded)
            {
                return NotesMcpResultMapper.Failure(parentResult.Error!);
            }

            parentIdFilter = parentResult.Value?.Id;
        }

        var normalizedOffset = Math.Max(0, offset ?? 0);
        var maxLimit = Math.Clamp(limit ?? 100, 1, mcpOptions.MaxListItemsLimit);
        var itemsPageResult = await notebookReadService.GetNotebookItemsPageAsync(
            notebook.Id,
            notebookContext.ActorId,
            search,
            cancellationToken,
            includeArchived,
            parentIdFilter,
            normalizedType == "all" ? null : normalizedType,
            normalizedOffset,
            maxLimit);
        if (!itemsPageResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(itemsPageResult.Error!);
        }

        var pagedItems = itemsPageResult.Value!.Items
            .Select(item => NotesMcpSupport.ToNotebookItemSummaryToolResponse(notebook, item))
            .ToList();

        var response = new ListNotebookItemsToolResponse(
            notebook.Id,
            notebook.Slug,
            notebook.Title,
            notebook.CanEdit,
            itemsPageResult.Value.TotalCount,
            normalizedOffset,
            pagedItems.Count,
            pagedItems);

        return NotesMcpResultMapper.Success(response, $"Listed {response.Items.Count} item(s) for notebook '{response.NotebookTitle}'.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.GetPage,
        Title = "Get Page",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(GetPageToolResponse))]
    [Description("Read one page by notebook slug and page path, including the stored TipTap JSON string and derived plain text. " + PathCompatibilityDescription)]
    public async Task<CallToolResult> GetPageAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path within the notebook. " + PathCompatibilityDescription)] string path,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var pageContextResult = await NotesMcpSupport.RequirePageSummaryContextAsync(
            notebookSlug,
            path,
            user,
            notebookReadService,
            cancellationToken,
            mcpOptions.RequiredReadScopes);
        if (!pageContextResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(pageContextResult.Error!);
        }

        var pageContext = pageContextResult.Value;
        var response = NotesMcpSupport.ToGetPageToolResponse(pageContext.Notebook, pageContext.Item);
        return NotesMcpResultMapper.Success(response, $"Page '{response.Title}' loaded.");
    }
}
