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
        Name = "notes.list_items",
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
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredReadScopes);
        if (!scopeResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(scopeResult.Error!);
        }

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookResult.Error!);
        }

        var notebook = notebookResult.Value!;
        var itemsResult = await notebookQueryService.GetNotebookItemsAsync(
            notebook.Id,
            NotesMcpSupport.GetCurrentUserId(user),
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
        Name = "notes.get_page",
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
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredReadScopes);
        if (!scopeResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(scopeResult.Error!);
        }

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookResult.Error!);
        }

        var pageResult = NotesMcpSupport.RequirePage(notebookResult.Value!, path);
        if (!pageResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(pageResult.Error!);
        }

        var response = NotesMcpSupport.ToGetPageToolResponse(notebookResult.Value!, pageResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Page '{response.Title}' loaded.");
    }

    [McpServerTool(
        Name = "notes.create_folder",
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
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredWriteScopes);
        if (!scopeResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(scopeResult.Error!);
        }

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookResult.Error!);
        }

        var parentResult = NotesMcpSupport.ResolveParent(notebookResult.Value!, parentPath);
        if (!parentResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(parentResult.Error!);
        }

        var createResult = await notebookCommandService.CreateNotebookItemAsync(
            notebookResult.Value!.Id,
            NotesMcpSupport.GetCurrentUserId(user),
            parentResult.Value?.Id,
            "folder",
            title,
            sortOrder ?? 0,
            null,
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, "notes.create_folder", notebookResult.Value!.Id, createResult.Value?.Id, createResult, cancellationToken);

        if (!createResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(createResult.Error!);
        }

        var response = NotesMcpSupport.ToCreateItemToolResponse(notebookResult.Value!, createResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Folder '{response.Title}' created.");
    }

    [McpServerTool(
        Name = "notes.create_page",
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
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredWriteScopes);
        if (!scopeResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(scopeResult.Error!);
        }

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookResult.Error!);
        }

        var parentResult = NotesMcpSupport.ResolveParent(notebookResult.Value!, parentPath);
        if (!parentResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(parentResult.Error!);
        }

        var createResult = await notebookCommandService.CreateNotebookItemAsync(
            notebookResult.Value!.Id,
            NotesMcpSupport.GetCurrentUserId(user),
            parentResult.Value?.Id,
            "page",
            title,
            sortOrder ?? 0,
            contentJson,
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, "notes.create_page", notebookResult.Value!.Id, createResult.Value?.Id, createResult, cancellationToken);

        if (!createResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(createResult.Error!);
        }

        var response = NotesMcpSupport.ToCreatePageToolResponse(notebookResult.Value!, createResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Page '{response.Title}' created.");
    }

    [McpServerTool(
        Name = "notes.update_page_content_json",
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
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredWriteScopes);
        if (!scopeResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(scopeResult.Error!);
        }

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookResult.Error!);
        }

        var pageResult = NotesMcpSupport.RequirePage(notebookResult.Value!, path);
        if (!pageResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(pageResult.Error!);
        }

        var updateResult = await notebookCommandService.UpdateNotebookItemAsync(
            notebookResult.Value!.Id,
            pageResult.Value!.Id,
            NotesMcpSupport.GetCurrentUserId(user),
            pageResult.Value.Title,
            default,
            null,
            contentJson,
            cancellationToken,
            expectedUpdatedAtUtc);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, "notes.update_page_content_json", notebookResult.Value!.Id, pageResult.Value!.Id, updateResult, cancellationToken);

        if (!updateResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(updateResult.Error!);
        }

        var response = NotesMcpSupport.ToUpdatePageContentToolResponse(notebookResult.Value!, updateResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Page '{response.Title}' content updated.");
    }

    [McpServerTool(
        Name = "notes.append_blocks_to_page",
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
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredWriteScopes);
        if (!scopeResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(scopeResult.Error!);
        }

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookResult.Error!);
        }

        var pageResult = NotesMcpSupport.RequirePage(notebookResult.Value!, path);
        if (!pageResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(pageResult.Error!);
        }

        if (blocks.ValueKind != JsonValueKind.Array)
        {
            return NotesMcpResultMapper.Failure(new NotesError(
                NotesFailureKind.Validation,
                "invalid_blocks",
                "Blocks must be a JSON array."));
        }

        var nextContentJson = NotesMcpSupport.AppendBlocks(pageResult.Value!.ContentJson, blocks);
        var updateResult = await notebookCommandService.UpdateNotebookItemAsync(
            notebookResult.Value!.Id,
            pageResult.Value!.Id,
            NotesMcpSupport.GetCurrentUserId(user),
            pageResult.Value.Title,
            default,
            null,
            nextContentJson,
            cancellationToken,
            expectedUpdatedAtUtc);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, "notes.append_blocks_to_page", notebookResult.Value!.Id, pageResult.Value!.Id, updateResult, cancellationToken);

        if (!updateResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(updateResult.Error!);
        }

        var response = NotesMcpSupport.ToUpdatePageContentToolResponse(notebookResult.Value!, updateResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Appended blocks to page '{response.Title}'.");
    }

    [McpServerTool(
        Name = "notes.rename_item",
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
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredWriteScopes);
        if (!scopeResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(scopeResult.Error!);
        }

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookResult.Error!);
        }

        var itemResult = NotesMcpSupport.RequireItem(notebookResult.Value!, path);
        if (!itemResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(itemResult.Error!);
        }

        var renameResult = await notebookCommandService.UpdateNotebookItemAsync(
            notebookResult.Value!.Id,
            itemResult.Value!.Id,
            NotesMcpSupport.GetCurrentUserId(user),
            title,
            default,
            null,
            default,
            cancellationToken,
            expectedUpdatedAtUtc);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, "notes.rename_item", notebookResult.Value!.Id, itemResult.Value!.Id, renameResult, cancellationToken);

        if (!renameResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(renameResult.Error!);
        }

        var refreshedNotebook = await notebookQueryService.GetNotebookBySlugAsync(notebookSlug, NotesMcpSupport.GetCurrentUserId(user), cancellationToken);
        if (!refreshedNotebook.Succeeded)
        {
            return NotesMcpResultMapper.Failure(refreshedNotebook.Error!);
        }

        var response = NotesMcpSupport.ToMoveItemToolResponse(refreshedNotebook.Value!, renameResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Item renamed to '{response.Title}'.");
    }

    [McpServerTool(
        Name = "notes.move_item",
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
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredWriteScopes);
        if (!scopeResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(scopeResult.Error!);
        }

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookResult.Error!);
        }

        var itemResult = NotesMcpSupport.RequireItem(notebookResult.Value!, path);
        if (!itemResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(itemResult.Error!);
        }

        var parentResult = NotesMcpSupport.ResolveParent(notebookResult.Value!, targetParentPath);
        if (!parentResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(parentResult.Error!);
        }

        var updateResult = await notebookCommandService.UpdateNotebookItemAsync(
            notebookResult.Value!.Id,
            itemResult.Value!.Id,
            NotesMcpSupport.GetCurrentUserId(user),
            itemResult.Value.Title,
            NotesMcpSupport.ToGuidJsonElement(parentResult.Value?.Id),
            sortOrder,
            default,
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, "notes.move_item", notebookResult.Value!.Id, itemResult.Value!.Id, updateResult, cancellationToken);

        if (!updateResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(updateResult.Error!);
        }

        var response = NotesMcpSupport.ToMoveItemToolResponse(notebookResult.Value!, updateResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Item '{response.Title}' moved to '{response.Path}'.");
    }

    [McpServerTool(
        Name = "notes.reorder_items",
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
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredWriteScopes);
        if (!scopeResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(scopeResult.Error!);
        }

        if (items.Count == 0)
        {
            return NotesMcpResultMapper.Failure(new NotesError(
                NotesFailureKind.Validation,
                "invalid_items",
                "At least one reorder item is required."));
        }

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookResult.Error!);
        }

        var reorderModels = new List<ReorderNotebookItemModel>();
        foreach (var item in items)
        {
            var resolvedItem = NotesMcpSupport.RequireItem(notebookResult.Value!, item.Path);
            if (!resolvedItem.Succeeded)
            {
                return NotesMcpResultMapper.Failure(resolvedItem.Error!);
            }

            var resolvedParent = NotesMcpSupport.ResolveParent(notebookResult.Value!, item.ParentPath);
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
            notebookResult.Value!.Id,
            NotesMcpSupport.GetCurrentUserId(user),
            reorderModels,
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, "notes.reorder_items", notebookResult.Value!.Id, null, reorderResult, cancellationToken);

        if (!reorderResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(reorderResult.Error!);
        }

        var response = new ReorderItemsToolResponse(
            notebookResult.Value!.Id,
            notebookResult.Value!.Slug,
            reorderResult.Value!.Select(NotesMcpSupport.ToNotebookItemToolResponse).ToList());

        return NotesMcpResultMapper.Success(response, $"Reordered {items.Count} item(s) in notebook '{response.NotebookSlug}'.");
    }

    [McpServerTool(
        Name = "notes.delete_item",
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
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredWriteScopes);
        if (!scopeResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(scopeResult.Error!);
        }

        var notebookResult = await notebookQueryService.GetNotebookBySlugAsync(
            notebookSlug,
            NotesMcpSupport.GetCurrentUserId(user),
            cancellationToken,
            includeArchived: true);
        if (!notebookResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookResult.Error!);
        }

        var itemResult = NotesMcpSupport.RequireItem(notebookResult.Value!, path);
        if (!itemResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(itemResult.Error!);
        }

        var deleteResult = await notebookCommandService.DeleteNotebookItemAsync(
            notebookResult.Value!.Id,
            itemResult.Value!.Id,
            NotesMcpSupport.GetCurrentUserId(user),
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, "notes.delete_item", notebookResult.Value!.Id, itemResult.Value!.Id, deleteResult, cancellationToken);

        if (!deleteResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(deleteResult.Error!);
        }

        var response = new DeleteItemToolResponse(
            notebookResult.Value!.Id,
            notebookResult.Value!.Slug,
            itemResult.Value!.Id,
            itemResult.Value!.Path,
            "deleted");

        return NotesMcpResultMapper.Success(response, $"Deleted item '{response.Path}'.");
    }

    [McpServerTool(
        Name = "notes.archive_item",
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
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredWriteScopes);
        if (!scopeResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(scopeResult.Error!);
        }

        var notebookResult = await NotesMcpSupport.RequireNotebookAsync(notebookSlug, user, notebookQueryService, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookResult.Error!);
        }

        var itemResult = NotesMcpSupport.RequireItem(notebookResult.Value!, path);
        if (!itemResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(itemResult.Error!);
        }

        var archiveResult = await notebookCommandService.ArchiveNotebookItemAsync(
            notebookResult.Value!.Id,
            itemResult.Value!.Id,
            NotesMcpSupport.GetCurrentUserId(user),
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, "notes.archive_item", notebookResult.Value!.Id, itemResult.Value!.Id, archiveResult, cancellationToken);

        if (!archiveResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(archiveResult.Error!);
        }

        var response = NotesMcpSupport.ToMoveItemToolResponse(notebookResult.Value!, archiveResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Archived item '{response.Path}'.");
    }

    [McpServerTool(
        Name = "notes.restore_item",
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
        var scopeResult = NotesMcpSupport.RequireScope(user, mcpOptions.RequiredWriteScopes);
        if (!scopeResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(scopeResult.Error!);
        }

        var notebookResult = await notebookQueryService.GetNotebookBySlugAsync(
            notebookSlug,
            NotesMcpSupport.GetCurrentUserId(user),
            cancellationToken,
            includeArchived: true);
        if (!notebookResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(notebookResult.Error!);
        }

        var itemResult = NotesMcpSupport.RequireItem(notebookResult.Value!, path);
        if (!itemResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(itemResult.Error!);
        }

        var restoreResult = await notebookCommandService.RestoreNotebookItemAsync(
            notebookResult.Value!.Id,
            itemResult.Value!.Id,
            NotesMcpSupport.GetCurrentUserId(user),
            cancellationToken);
        await NotesMcpSupport.AuditWriteAsync(auditService, user, "notes.restore_item", notebookResult.Value!.Id, itemResult.Value!.Id, restoreResult, cancellationToken);

        if (!restoreResult.Succeeded)
        {
            return NotesMcpResultMapper.Failure(restoreResult.Error!);
        }

        var refreshedNotebook = await notebookQueryService.GetNotebookBySlugAsync(notebookSlug, NotesMcpSupport.GetCurrentUserId(user), cancellationToken);
        if (!refreshedNotebook.Succeeded)
        {
            return NotesMcpResultMapper.Failure(refreshedNotebook.Error!);
        }

        var response = NotesMcpSupport.ToMoveItemToolResponse(refreshedNotebook.Value!, restoreResult.Value!);
        return NotesMcpResultMapper.Success(response, $"Restored item '{response.Path}'.");
    }
}
