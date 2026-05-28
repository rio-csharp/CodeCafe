using CodeCafe.Application.Common.Interfaces;
using CodeCafe.Application.Notes;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;

namespace CodeCafe.WebApi.Mcp;

[McpServerToolType]
public sealed class NotesMcpItemTools
{
    [McpServerTool(
        Name = NotesMcpToolNames.ListItems,
        Title = "List Notebook Items",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(ListNotebookItemsToolResponse))]
    [Description("List folder and page items in a notebook visible to the authenticated actor.")]
    public async Task<CallToolResult> ListItemsAsync(
        [Description("The notebook slug.")] string notebookSlug,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        CancellationToken cancellationToken,
        IOptions<McpOptions> mcpOptionsAccessor,
        [Description("Optional search term to filter notebook items.")] string? search = null)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var notebookContextResult = await NotesMcpSupport.RequireNotebookContextAsync(
            notebookSlug,
            user,
            notebookQueryService,
            cancellationToken,
            mcpOptions.RequiredReadScopes);
        if (!notebookContextResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookContextResult.Error!);
        }

        var notebookContext = notebookContextResult.Value;
        var notebook = notebookContext.Notebook;
        var itemsResult = await notebookQueryService.GetNotebookItemsAsync(
            notebook.Id,
            notebookContext.ActorId,
            search,
            cancellationToken);

        if (!itemsResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(itemsResult.Error!);
        }

        var response = new ListNotebookItemsToolResponse(
            notebook.Id,
            notebook.Slug,
            notebook.Title,
            notebook.CanEdit,
            itemsResult.Value!.Select(NotesMcpSupport.ToNotebookItemToolResponse).ToList());

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
    [Description("Read one page by notebook slug and page path.")]
    public async Task<CallToolResult> GetPageAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path within the notebook.")] string path,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var pageContextResult = await NotesMcpSupport.RequirePageContextAsync(
            notebookSlug,
            path,
            user,
            notebookQueryService,
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

    [McpServerTool(
        Name = NotesMcpToolNames.CreateFolder,
        Title = "Create Folder",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(CreateItemToolResponse))]
    [Description("Create a folder in a notebook under an optional parent folder path.")]
    public async Task<CallToolResult> CreateFolderAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The folder title.")] string title,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpAuditService auditService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional parent folder path. Null creates the folder at the notebook root.")] string? parentPath = null,
        [Description("Sort order within the parent folder.")] int? sortOrder = null)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var notebookContextResult = await NotesMcpSupport.RequireNotebookContextAsync(
            notebookSlug,
            user,
            notebookQueryService,
            cancellationToken,
            mcpOptions.RequiredWriteScopes);
        if (!notebookContextResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookContextResult.Error!);
        }

        var notebookContext = notebookContextResult.Value;
        var parentResult = NotesMcpSupport.ResolveParent(notebookContext.Notebook, parentPath);
        if (!parentResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(parentResult.Error!);
        }

        var createResult = await notebookCommandService.CreateNotebookItemAsync(
            notebookContext.Notebook.Id,
            notebookContext.ActorId,
            parentResult.Value?.Id,
            "folder",
            title,
            sortOrder ?? 0,
            null,
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, NotesMcpToolNames.CreateFolder, notebookContext.Notebook.Id, createResult.Value?.Id, createResult, cancellationToken);

        if (!createResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(createResult.Error!);
        }

        var response = NotesMcpSupport.ToCreateItemToolResponse(notebookContext.Notebook, createResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Folder '{response.Title}' created.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.CreatePage,
        Title = "Create Page",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(CreatePageToolResponse))]
    [Description("Create a page in a notebook under an optional parent folder path.")]
    public async Task<CallToolResult> CreatePageAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page title.")] string title,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpAuditService auditService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional parent folder path. Null creates the page at the notebook root.")] string? parentPath = null,
        [Description("Sort order within the parent folder.")] int? sortOrder = null,
        [Description("Optional TipTap JSON document for the page content.")] JsonElement? contentJson = null)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var notebookContextResult = await NotesMcpSupport.RequireNotebookContextAsync(
            notebookSlug,
            user,
            notebookQueryService,
            cancellationToken,
            mcpOptions.RequiredWriteScopes);
        if (!notebookContextResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookContextResult.Error!);
        }

        var notebookContext = notebookContextResult.Value;
        var parentResult = NotesMcpSupport.ResolveParent(notebookContext.Notebook, parentPath);
        if (!parentResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(parentResult.Error!);
        }

        var createResult = await notebookCommandService.CreateNotebookItemAsync(
            notebookContext.Notebook.Id,
            notebookContext.ActorId,
            parentResult.Value?.Id,
            "page",
            title,
            sortOrder ?? 0,
            contentJson,
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, NotesMcpToolNames.CreatePage, notebookContext.Notebook.Id, createResult.Value?.Id, createResult, cancellationToken);

        if (!createResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(createResult.Error!);
        }

        var response = NotesMcpSupport.ToCreatePageToolResponse(notebookContext.Notebook, createResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Page '{response.Title}' created.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.UpdatePageContentJson,
        Title = "Update Page Content",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(UpdatePageContentToolResponse))]
    [Description("Replace a page TipTap JSON document.")]
    public async Task<CallToolResult> UpdatePageContentJsonAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path.")] string path,
        [Description("The full TipTap JSON document to store.")] JsonElement contentJson,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpAuditService auditService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional expected updated timestamp in UTC for conflict detection.")] DateTimeOffset? expectedUpdatedAtUtc = null)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var pageContextResult = await NotesMcpSupport.RequirePageContextAsync(
            notebookSlug,
            path,
            user,
            notebookQueryService,
            cancellationToken,
            mcpOptions.RequiredWriteScopes);
        if (!pageContextResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(pageContextResult.Error!);
        }

        var pageContext = pageContextResult.Value;
        var updateResult = await notebookCommandService.UpdateNotebookItemAsync(
            pageContext.Notebook.Id,
            pageContext.Item.Id,
            pageContext.ActorId,
            pageContext.Item.Title,
            default,
            null,
            contentJson,
            cancellationToken,
            expectedUpdatedAtUtc);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, NotesMcpToolNames.UpdatePageContentJson, pageContext.Notebook.Id, pageContext.Item.Id, updateResult, cancellationToken);

        if (!updateResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(updateResult.Error!);
        }

        var response = NotesMcpSupport.ToUpdatePageContentToolResponse(pageContext.Notebook, updateResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Page '{response.Title}' content updated.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.AppendBlocksToPage,
        Title = "Append Blocks To Page",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(UpdatePageContentToolResponse))]
    [Description("Append TipTap block nodes to an existing page document.")]
    public async Task<CallToolResult> AppendBlocksToPageAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The page path.")] string path,
        [Description("The TipTap block nodes to append.")] JsonElement blocks,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpAuditService auditService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional expected updated timestamp in UTC for conflict detection.")] DateTimeOffset? expectedUpdatedAtUtc = null)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var pageContextResult = await NotesMcpSupport.RequirePageContextAsync(
            notebookSlug,
            path,
            user,
            notebookQueryService,
            cancellationToken,
            mcpOptions.RequiredWriteScopes);
        if (!pageContextResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(pageContextResult.Error!);
        }

        if (blocks.ValueKind != JsonValueKind.Array)
        {
            return NotesMcpResultMapper.Failure(new NotesError(
                NotesFailureKind.Validation,
                "invalid_blocks",
                "Blocks must be a JSON array."));
        }

        var pageContext = pageContextResult.Value;
        var nextContentJson = NotesMcpSupport.AppendBlocks(pageContext.Item.ContentJson, blocks);
        var updateResult = await notebookCommandService.UpdateNotebookItemAsync(
            pageContext.Notebook.Id,
            pageContext.Item.Id,
            pageContext.ActorId,
            pageContext.Item.Title,
            default,
            null,
            nextContentJson,
            cancellationToken,
            expectedUpdatedAtUtc);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, NotesMcpToolNames.AppendBlocksToPage, pageContext.Notebook.Id, pageContext.Item.Id, updateResult, cancellationToken);

        if (!updateResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(updateResult.Error!);
        }

        var response = NotesMcpSupport.ToUpdatePageContentToolResponse(pageContext.Notebook, updateResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Appended blocks to page '{response.Title}'.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.RenameItem,
        Title = "Rename Item",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(MoveItemToolResponse))]
    [Description("Rename a page or folder while keeping its current parent and content.")]
    public async Task<CallToolResult> RenameItemAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The current item path.")] string path,
        [Description("The new title for the page or folder.")] string title,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpAuditService auditService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional expected updated timestamp in UTC for conflict detection.")] DateTimeOffset? expectedUpdatedAtUtc = null)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var itemContextResult = await NotesMcpSupport.RequireItemContextAsync(
            notebookSlug,
            path,
            user,
            notebookQueryService,
            cancellationToken,
            mcpOptions.RequiredWriteScopes);
        if (!itemContextResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(itemContextResult.Error!);
        }

        var itemContext = itemContextResult.Value;
        var renameResult = await notebookCommandService.UpdateNotebookItemAsync(
            itemContext.Notebook.Id,
            itemContext.Item.Id,
            itemContext.ActorId,
            title,
            default,
            null,
            default,
            cancellationToken,
            expectedUpdatedAtUtc);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, NotesMcpToolNames.RenameItem, itemContext.Notebook.Id, itemContext.Item.Id, renameResult, cancellationToken);

        if (!renameResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(renameResult.Error!);
        }

        var refreshedNotebook = await notebookQueryService.GetNotebookBySlugAsync(notebookSlug, itemContext.ActorId, cancellationToken);
        if (!refreshedNotebook.Succeeded)
        {
            return NotesMcpResultMapper.Failure(refreshedNotebook.Error!);
        }

        var response = NotesMcpSupport.ToMoveItemToolResponse(refreshedNotebook.Value!, renameResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Item renamed to '{response.Title}'.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.MoveItem,
        Title = "Move Item",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(MoveItemToolResponse))]
    [Description("Move a page or folder to a new parent folder path or to the notebook root.")]
    public async Task<CallToolResult> MoveItemAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The current item path.")] string path,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpAuditService auditService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("The target parent folder path. Null moves the item to the notebook root.")] string? targetParentPath = null,
        [Description("Optional new sort order.")] int? sortOrder = null)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var itemContextResult = await NotesMcpSupport.RequireItemContextAsync(
            notebookSlug,
            path,
            user,
            notebookQueryService,
            cancellationToken,
            mcpOptions.RequiredWriteScopes);
        if (!itemContextResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(itemContextResult.Error!);
        }

        var itemContext = itemContextResult.Value;
        var parentResult = NotesMcpSupport.ResolveParent(itemContext.Notebook, targetParentPath);
        if (!parentResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(parentResult.Error!);
        }

        var updateResult = await notebookCommandService.UpdateNotebookItemAsync(
            itemContext.Notebook.Id,
            itemContext.Item.Id,
            itemContext.ActorId,
            itemContext.Item.Title,
            NotesMcpSupport.ToGuidJsonElement(parentResult.Value?.Id),
            sortOrder,
            default,
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, NotesMcpToolNames.MoveItem, itemContext.Notebook.Id, itemContext.Item.Id, updateResult, cancellationToken);

        if (!updateResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(updateResult.Error!);
        }

        var response = NotesMcpSupport.ToMoveItemToolResponse(itemContext.Notebook, updateResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Item '{response.Title}' moved to '{response.Path}'.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.ReorderItems,
        Title = "Reorder Items",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(ReorderItemsToolResponse))]
    [Description("Batch reorder and optionally move items within a notebook.")]
    public async Task<CallToolResult> ReorderItemsAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The set of item reorder operations.")] IReadOnlyList<ReorderNotesItemRequest> items,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpAuditService auditService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        if (items.Count == 0)
        {
            return NotesMcpResultMapper.Failure(new NotesError(
                NotesFailureKind.Validation,
                "invalid_items",
                "At least one reorder item is required."));
        }

        var notebookContextResult = await NotesMcpSupport.RequireNotebookContextAsync(
            notebookSlug,
            user,
            notebookQueryService,
            cancellationToken,
            mcpOptions.RequiredWriteScopes);
        if (!notebookContextResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookContextResult.Error!);
        }

        var notebookContext = notebookContextResult.Value;
        var reorderModels = new List<ReorderNotebookItemModel>();
        foreach (var item in items)
        {
            var resolvedItem = NotesMcpSupport.RequireItem(notebookContext.Notebook, item.Path);
            if (!resolvedItem.Succeeded)
            {
                return NotesMcpResultMapper.Failure(resolvedItem.Error!);
            }

            var resolvedParent = NotesMcpSupport.ResolveParent(notebookContext.Notebook, item.ParentPath);
            if (!resolvedParent.Succeeded)
            {
                return NotesMcpResultMapper.Failure(resolvedParent.Error!);
            }

            reorderModels.Add(new ReorderNotebookItemModel(
                resolvedItem.Value!.Id,
                resolvedParent.Value?.Id,
                item.SortOrder));
        }

        var reorderResult = await notebookCommandService.ReorderNotebookItemsAsync(
            notebookContext.Notebook.Id,
            notebookContext.ActorId,
            reorderModels,
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, NotesMcpToolNames.ReorderItems, notebookContext.Notebook.Id, null, reorderResult, cancellationToken);

        if (!reorderResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(reorderResult.Error!);
        }

        var response = new ReorderItemsToolResponse(
            notebookContext.Notebook.Id,
            notebookContext.Notebook.Slug,
            reorderResult.Value!.Select(NotesMcpSupport.ToNotebookItemToolResponse).ToList());

        return NotesMcpResultMapper.Success(response, $"Reordered {items.Count} item(s) in notebook '{response.NotebookSlug}'.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.DeleteItem,
        Title = "Delete Item",
        ReadOnly = false,
        Destructive = true,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(DeleteItemToolResponse))]
    [Description("Delete a page or folder and its descendants from a notebook.")]
    public async Task<CallToolResult> DeleteItemAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The item path.")] string path,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpAuditService auditService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var itemContextResult = await NotesMcpSupport.RequireItemContextAsync(
            notebookSlug,
            path,
            user,
            notebookQueryService,
            cancellationToken,
            mcpOptions.RequiredWriteScopes,
            includeArchived: true);
        if (!itemContextResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(itemContextResult.Error!);
        }

        var itemContext = itemContextResult.Value;
        var deleteResult = await notebookCommandService.DeleteNotebookItemAsync(
            itemContext.Notebook.Id,
            itemContext.Item.Id,
            itemContext.ActorId,
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, NotesMcpToolNames.DeleteItem, itemContext.Notebook.Id, itemContext.Item.Id, deleteResult, cancellationToken);

        if (!deleteResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(deleteResult.Error!);
        }

        var response = new DeleteItemToolResponse(
            itemContext.Notebook.Id,
            itemContext.Notebook.Slug,
            itemContext.Item.Id,
            itemContext.Item.Path,
            "deleted");

        return NotesMcpResultMapper.Success(response, $"Deleted item '{response.Path}'.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.ArchiveItem,
        Title = "Archive Item",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(MoveItemToolResponse))]
    [Description("Archive a page or folder and its descendants.")]
    public async Task<CallToolResult> ArchiveItemAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The item path.")] string path,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpAuditService auditService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var itemContextResult = await NotesMcpSupport.RequireItemContextAsync(
            notebookSlug,
            path,
            user,
            notebookQueryService,
            cancellationToken,
            mcpOptions.RequiredWriteScopes);
        if (!itemContextResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(itemContextResult.Error!);
        }

        var itemContext = itemContextResult.Value;
        var archiveResult = await notebookCommandService.ArchiveNotebookItemAsync(
            itemContext.Notebook.Id,
            itemContext.Item.Id,
            itemContext.ActorId,
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, NotesMcpToolNames.ArchiveItem, itemContext.Notebook.Id, itemContext.Item.Id, archiveResult, cancellationToken);

        if (!archiveResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(archiveResult.Error!);
        }

        var response = NotesMcpSupport.ToMoveItemToolResponse(itemContext.Notebook, archiveResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Archived item '{response.Path}'.");
    }

    [McpServerTool(
        Name = NotesMcpToolNames.RestoreItem,
        Title = "Restore Item",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(MoveItemToolResponse))]
    [Description("Restore an archived page or folder and its descendants.")]
    public async Task<CallToolResult> RestoreItemAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The archived item path.")] string path,
        ClaimsPrincipal user,
        INotebookQueryService notebookQueryService,
        INotebookCommandService notebookCommandService,
        IMcpAuditService auditService,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var mcpOptions = mcpOptionsAccessor.Value;
        var itemContextResult = await NotesMcpSupport.RequireItemContextAsync(
            notebookSlug,
            path,
            user,
            notebookQueryService,
            cancellationToken,
            mcpOptions.RequiredWriteScopes,
            includeArchived: true);
        if (!itemContextResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(itemContextResult.Error!);
        }

        var itemContext = itemContextResult.Value;
        var restoreResult = await notebookCommandService.RestoreNotebookItemAsync(
            itemContext.Notebook.Id,
            itemContext.Item.Id,
            itemContext.ActorId,
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, NotesMcpToolNames.RestoreItem, itemContext.Notebook.Id, itemContext.Item.Id, restoreResult, cancellationToken);

        if (!restoreResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(restoreResult.Error!);
        }

        var refreshedNotebook = await notebookQueryService.GetNotebookBySlugAsync(notebookSlug, itemContext.ActorId, cancellationToken);
        if (!refreshedNotebook.Succeeded)
        {
            return NotesMcpResultMapper.Failure(refreshedNotebook.Error!);
        }

        var response = NotesMcpSupport.ToMoveItemToolResponse(refreshedNotebook.Value!, restoreResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Restored item '{response.Path}'.");
    }
}
