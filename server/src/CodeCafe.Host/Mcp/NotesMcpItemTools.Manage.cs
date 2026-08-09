using CodeCafe.Infrastructure.Mcp;
using CodeCafe.Application.Mcp;
using CodeCafe.Application.Notes;
using CodeCafe.Application.Notes.Commands.ArchiveNotebookItem;
using CodeCafe.Application.Notes.Commands.DeleteNotebookItem;
using CodeCafe.Application.Notes.Commands.ReorderNotebookItems;
using CodeCafe.Application.Notes.Commands.RestoreNotebookItem;
using CodeCafe.Application.Notes.Commands.UpdateNotebookItem;
using CodeCafe.Application.Common.Configuration;
using MediatR;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Claims;

namespace CodeCafe.Host.Mcp;

public sealed partial class NotesMcpItemTools
{
    [McpServerTool(
        Name = NotesMcpToolNames.RenameItem,
        Title = "Rename Item",
        ReadOnly = false,
        Destructive = true,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(MoveItemToolResponse))]
    [Description("Rename a page or folder while keeping its current parent and content. " + PathCompatibilityDescription)]
    public async Task<CallToolResult> RenameItemAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The current item path. " + PathCompatibilityDescription)] string path,
        [Description("The new title for the page or folder.")] string title,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        ISender sender,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("Optional expected updated timestamp in UTC for conflict detection.")] DateTimeOffset? expectedUpdatedAtUtc = null)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.RenameItem,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var itemContextResult = await NotesMcpSupport.RequireItemSummaryContextAsync(
                    notebookSlug,
                    path,
                    user,
                    notebookReadService,
                    ct,
                    mcpOptions.RequiredWriteScopes,
                    includeArchived: true);
                if (!itemContextResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(itemContextResult.Error!);
                }

                var itemContext = itemContextResult.Value;
                var renameResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new UpdateNotebookItemCommand(
                        itemContext.Notebook.Id,
                        itemContext.Item.Id,
                        itemContext.ActorId,
                        title,
                        default,
                        null,
                        default,
                        expectedUpdatedAtUtc),
                    ct);
                if (!renameResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(
                        renameResult.Error!,
                        itemContext.Notebook.Id,
                        itemContext.Item.Id);
                }

                var response = NotesMcpSupport.ToMoveItemToolResponse(itemContext.Notebook, renameResult.Value!);
                return McpMutationResult<MoveItemToolResponse>.Success(
                    response,
                    $"Item renamed to '{response.Title}'.",
                    itemContext.Notebook.Id,
                    itemContext.Item.Id);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.MoveItem,
        Title = "Move Item",
        ReadOnly = false,
        Destructive = true,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(MoveItemToolResponse))]
    [Description("Move a page or folder to a new parent folder path or to the notebook root. " + PathCompatibilityDescription)]
    public async Task<CallToolResult> MoveItemAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The current item path. " + PathCompatibilityDescription)] string path,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        ISender sender,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken,
        [Description("The target parent folder path. Null moves the item to the notebook root. " + PathCompatibilityDescription)] string? targetParentPath = null,
        [Description("Optional new sort order.")] int? sortOrder = null)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.MoveItem,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var itemContextResult = await NotesMcpSupport.RequireItemSummaryContextAsync(
                    notebookSlug,
                    path,
                    user,
                    notebookReadService,
                    ct,
                    mcpOptions.RequiredWriteScopes,
                    includeArchived: true);
                if (!itemContextResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(itemContextResult.Error!);
                }

                var itemContext = itemContextResult.Value;
                var parentResult = await NotesMcpSupport.ResolveParentAsync(
                    itemContext.Notebook,
                    targetParentPath,
                    itemContext.ActorId,
                    notebookReadService,
                    ct,
                    includeArchived: true);
                if (!parentResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(
                        parentResult.Error!,
                        itemContext.Notebook.Id,
                        itemContext.Item.Id);
                }

                var updateResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new UpdateNotebookItemCommand(
                        itemContext.Notebook.Id,
                        itemContext.Item.Id,
                        itemContext.ActorId,
                        itemContext.Item.Title,
                        NotesMcpSupport.ToGuidJsonElement(parentResult.Value?.Id),
                        sortOrder,
                        default),
                    ct);
                if (!updateResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(
                        updateResult.Error!,
                        itemContext.Notebook.Id,
                        itemContext.Item.Id);
                }

                var response = NotesMcpSupport.ToMoveItemToolResponse(itemContext.Notebook, updateResult.Value!);
                return McpMutationResult<MoveItemToolResponse>.Success(
                    response,
                    $"Item '{response.Title}' moved to '{response.Path}'.",
                    itemContext.Notebook.Id,
                    itemContext.Item.Id);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.ReorderItems,
        Title = "Reorder Items",
        ReadOnly = false,
        Destructive = true,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(ReorderItemsToolResponse))]
    [Description("Batch reorder and optionally move items within a notebook.")]
    public async Task<CallToolResult> ReorderItemsAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The set of item reorder operations.")] IReadOnlyList<ReorderNotesItemRequest> items,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        ISender sender,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.ReorderItems,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                if (items.Count == 0)
                {
                    return McpMutationResult<ReorderItemsToolResponse>.Failure(new NotesError(
                        NotesFailureKind.Validation,
                        "invalid_items",
                        "At least one reorder item is required."));
                }

                var notebookContextResult = await NotesMcpSupport.RequireNotebookContextAsync(
                    notebookSlug,
                    user,
                    notebookReadService,
                    ct,
                    mcpOptions.RequiredWriteScopes);
                if (!notebookContextResult.Succeeded)
                {
                    return McpMutationResult<ReorderItemsToolResponse>.Failure(notebookContextResult.Error!);
                }

                var notebookContext = notebookContextResult.Value;
                var reorderModels = new List<ReorderNotebookItemModel>();
                foreach (var item in items)
                {
                    var itemResult = NotesMcpSupport.RequireItem(notebookContext.Notebook, item.Path);
                    if (!itemResult.Succeeded)
                    {
                        return McpMutationResult<ReorderItemsToolResponse>.Failure(new NotesError(
                            NotesFailureKind.NotFound,
                            "notebook_item_not_found",
                            "Notebook item was not found."),
                            notebookContext.Notebook.Id);
                    }

                    var resolvedItem = itemResult.Value!;
                    var resolvedParent = NotesMcpSupport.ResolveParent(notebookContext.Notebook, item.ParentPath);
                    if (!resolvedParent.Succeeded)
                    {
                        return McpMutationResult<ReorderItemsToolResponse>.Failure(
                            resolvedParent.Error!,
                            notebookContext.Notebook.Id);
                    }

                    reorderModels.Add(new ReorderNotebookItemModel(
                        resolvedItem.Id,
                        resolvedParent.Value?.Id,
                        item.SortOrder));
                }

                var reorderResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new ReorderNotebookItemsCommand(
                        notebookContext.Notebook.Id,
                        notebookContext.ActorId,
                        reorderModels),
                    ct);
                if (!reorderResult.Succeeded)
                {
                    return McpMutationResult<ReorderItemsToolResponse>.Failure(
                        reorderResult.Error!,
                        notebookContext.Notebook.Id);
                }

                var response = new ReorderItemsToolResponse(
                    notebookContext.Notebook.Id,
                    notebookContext.Notebook.Slug,
                    reorderResult.Value!.Select(item => NotesMcpSupport.ToNotebookItemSummaryToolResponse(notebookContext.Notebook, item)).ToList());

                return McpMutationResult<ReorderItemsToolResponse>.Success(
                    response,
                    $"Reordered {items.Count} item(s) in notebook '{response.NotebookSlug}'.",
                    notebookContext.Notebook.Id,
                    itemId: null);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.DeleteItem,
        Title = "Delete Item",
        ReadOnly = false,
        Destructive = true,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(DeleteItemToolResponse))]
    [Description("Delete a page or folder and its descendants from a notebook. " + PathCompatibilityDescription)]
    public async Task<CallToolResult> DeleteItemAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The item path. " + PathCompatibilityDescription)] string path,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        ISender sender,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.DeleteItem,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var itemContextResult = await NotesMcpSupport.RequireItemSummaryContextAsync(
                    notebookSlug,
                    path,
                    user,
                    notebookReadService,
                    ct,
                    mcpOptions.RequiredWriteScopes,
                    includeArchived: true);
                if (!itemContextResult.Succeeded)
                {
                    return McpMutationResult<DeleteItemToolResponse>.Failure(itemContextResult.Error!);
                }

                var itemContext = itemContextResult.Value;
                var deleteResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new DeleteNotebookItemCommand(
                        itemContext.Notebook.Id,
                        itemContext.Item.Id,
                        itemContext.ActorId),
                    ct);
                if (!deleteResult.Succeeded)
                {
                    return McpMutationResult<DeleteItemToolResponse>.Failure(
                        deleteResult.Error!,
                        itemContext.Notebook.Id,
                        itemContext.Item.Id);
                }

                var response = new DeleteItemToolResponse(
                    itemContext.Notebook.Id,
                    itemContext.Notebook.Slug,
                    itemContext.Item.Id,
                    itemContext.Item.Path,
                    "deleted");

                return McpMutationResult<DeleteItemToolResponse>.Success(
                    response,
                    $"Deleted item '{response.Path}'.",
                    itemContext.Notebook.Id,
                    itemContext.Item.Id);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.ArchiveItem,
        Title = "Archive Item",
        ReadOnly = false,
        Destructive = true,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(MoveItemToolResponse))]
    [Description("Archive a page or folder and its descendants. " + PathCompatibilityDescription)]
    public async Task<CallToolResult> ArchiveItemAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The item path. " + PathCompatibilityDescription)] string path,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        ISender sender,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.ArchiveItem,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var itemContextResult = await NotesMcpSupport.RequireItemSummaryContextAsync(
                    notebookSlug,
                    path,
                    user,
                    notebookReadService,
                    ct,
                    mcpOptions.RequiredWriteScopes,
                    includeArchived: true);
                if (!itemContextResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(itemContextResult.Error!);
                }

                var itemContext = itemContextResult.Value;
                var archiveResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new ArchiveNotebookItemCommand(
                        itemContext.Notebook.Id,
                        itemContext.Item.Id,
                        itemContext.ActorId),
                    ct);
                if (!archiveResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(
                        archiveResult.Error!,
                        itemContext.Notebook.Id,
                        itemContext.Item.Id);
                }

                var response = NotesMcpSupport.ToMoveItemToolResponse(itemContext.Notebook, archiveResult.Value!);
                return McpMutationResult<MoveItemToolResponse>.Success(
                    response,
                    $"Archived item '{response.Path}'.",
                    itemContext.Notebook.Id,
                    itemContext.Item.Id);
            },
            cancellationToken);
    }

    [McpServerTool(
        Name = NotesMcpToolNames.RestoreItem,
        Title = "Restore Item",
        ReadOnly = false,
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true,
        OutputSchemaType = typeof(MoveItemToolResponse))]
    [Description("Restore an archived page or folder and its descendants. " + PathCompatibilityDescription)]
    public async Task<CallToolResult> RestoreItemAsync(
        [Description("The notebook slug.")] string notebookSlug,
        [Description("The archived item path. " + PathCompatibilityDescription)] string path,
        ClaimsPrincipal user,
        INotebookReadService notebookReadService,
        ISender sender,
        IMcpMutationExecutor mutationExecutor,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        return await mutationExecutor.ExecuteAsync(
            user,
            NotesMcpToolNames.RestoreItem,
            async ct =>
            {
                var mcpOptions = mcpOptionsAccessor.Value;
                var itemContextResult = await NotesMcpSupport.RequireItemSummaryContextAsync(
                    notebookSlug,
                    path,
                    user,
                    notebookReadService,
                    ct,
                    mcpOptions.RequiredWriteScopes,
                    includeArchived: true);
                if (!itemContextResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(itemContextResult.Error!);
                }

                var itemContext = itemContextResult.Value;
                var restoreResult = await NotesMcpCommandSender.SendAsync(
                    sender,
                    new RestoreNotebookItemCommand(
                        itemContext.Notebook.Id,
                        itemContext.Item.Id,
                        itemContext.ActorId),
                    ct);
                if (!restoreResult.Succeeded)
                {
                    return McpMutationResult<MoveItemToolResponse>.Failure(
                        restoreResult.Error!,
                        itemContext.Notebook.Id,
                        itemContext.Item.Id);
                }

                var response = NotesMcpSupport.ToMoveItemToolResponse(itemContext.Notebook, restoreResult.Value!);
                return McpMutationResult<MoveItemToolResponse>.Success(
                    response,
                    $"Restored item '{response.Path}'.",
                    itemContext.Notebook.Id,
                    itemContext.Item.Id);
            },
            cancellationToken);
    }
}
