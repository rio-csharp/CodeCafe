using CodeCafe.Application.Common.Interfaces;
using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CodeCafe.Infrastructure.Notes;

public sealed class NotebookItemMutationService(
    ApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    ITipTapContentService tipTapContentService) : INotebookItemMutationService
{
    public async Task<NotesResult<NotebookItemModel>> CreateNotebookItemAsync(
        Guid notebookId,
        Guid currentUserId,
        Guid? parentId,
        string type,
        string title,
        int sortOrder,
        JsonElement? contentJson,
        CancellationToken cancellationToken)
    {
        if (await EnsureOwnedNotebookAsync(notebookId, currentUserId, cancellationToken) is { } accessError)
        {
            return NotesResult<NotebookItemModel>.Failure(accessError.Kind, accessError.Code, accessError.Message);
        }

        if (!NotebookInput.TryParseItemType(type, out var itemType))
        {
            return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "invalid_item_type", "Item type must be folder or page.");
        }

        var parent = await GetParentItemAsync(notebookId, parentId, cancellationToken);
        if (parentId is not null && parent is null)
        {
            return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "invalid_parent", "Parent item was not found in this notebook.");
        }

        if (parent is not null && parent.Type != NotebookItemType.Folder)
        {
            return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "invalid_parent", "Parent item must be a folder.");
        }

        var trimmedTitle = title.Trim();
        var path = await GenerateItemPathAsync(notebookId, parent?.Path, trimmedTitle, null, cancellationToken);
        var normalizedContent = NotesResult<TipTapContentModel>.Success(new TipTapContentModel(null, null));
        if (itemType == NotebookItemType.Page)
        {
            normalizedContent = tipTapContentService.NormalizePageContent(contentJson, trimmedTitle);
            if (!normalizedContent.Succeeded)
            {
                return NotesResult<NotebookItemModel>.Failure(
                    normalizedContent.Error!.Kind,
                    normalizedContent.Error.Code,
                    normalizedContent.Error.Message,
                    normalizedContent.Error.Field,
                    normalizedContent.Error.Details);
            }
        }

        var item = new NotebookItem
        {
            Id = Guid.NewGuid(),
            NotebookId = notebookId,
            ParentId = parentId,
            Type = itemType,
            Title = trimmedTitle,
            Slug = path.Split('/')[^1],
            Path = path,
            SortOrder = sortOrder,
            ContentFormat = itemType == NotebookItemType.Page ? NotesSupport.PageContentFormat : null,
            ContentJson = itemType == NotebookItemType.Page ? normalizedContent.Value!.ContentJson : null,
            PlainTextContent = itemType == NotebookItemType.Page ? normalizedContent.Value!.PlainTextContent : null
        };

        dbContext.NotebookItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NotesResult<NotebookItemModel>.Success(NotesSupport.ToItemModel(item));
    }

    public async Task<NotesResult<NotebookItemModel>> UpdateNotebookItemAsync(
        Guid notebookId,
        Guid itemId,
        Guid currentUserId,
        string title,
        JsonElement parentId,
        int? sortOrder,
        JsonElement contentJson,
        CancellationToken cancellationToken,
        DateTimeOffset? expectedUpdatedAtUtc = null)
    {
        if (await EnsureOwnedNotebookAsync(notebookId, currentUserId, cancellationToken) is { } accessError)
        {
            return NotesResult<NotebookItemModel>.Failure(accessError.Kind, accessError.Code, accessError.Message);
        }

        var item = await dbContext.NotebookItems.SingleOrDefaultAsync(
            existingItem => existingItem.NotebookId == notebookId && existingItem.Id == itemId && !existingItem.IsArchived,
            cancellationToken);
        if (item is null)
        {
            return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "Notebook item was not found.");
        }

        if (expectedUpdatedAtUtc.HasValue && item.UpdatedAtUtc != expectedUpdatedAtUtc.Value)
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Conflict,
                "content_conflict",
                "The page changed after the expected timestamp.");
        }

        var notebookItems = await GetNotebookStructureAsync(notebookId, includeArchived: false, cancellationToken);
        var requestedParentId = item.ParentId;
        var currentParent = NotebookItemTree.FindRequestedParent(notebookItems, item.Id, item.ParentId);
        var nextParent = currentParent;

        if (parentId.ValueKind != JsonValueKind.Undefined)
        {
            if (!NotebookInput.TryParseOptionalGuid(parentId, out requestedParentId))
            {
                return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "invalid_parent", "ParentId must be a GUID or null.");
            }

            nextParent = NotebookItemTree.FindRequestedParent(notebookItems, item.Id, requestedParentId);
            if (requestedParentId is not null && nextParent is null)
            {
                return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "invalid_parent", "Parent item was not found in this notebook.");
            }

            if (nextParent is not null && nextParent.Type != NotebookItemType.Folder)
            {
                return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "invalid_parent", "Parent item must be a folder.");
            }

            if (nextParent is not null && NotebookItemTree.WouldCreateCycle(notebookItems, item.Id, nextParent.Id))
            {
                return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "invalid_parent", "Item cannot be moved into itself or its descendants.");
            }
        }

        var oldPath = item.Path;
        var parentPath = nextParent?.Path;
        var trimmedTitle = title.Trim();
        var nextPath = NotebookItemTree.GeneratePath(notebookItems, parentPath, trimmedTitle, item.Id);
        item.UpdateStructure(requestedParentId, trimmedTitle, nextPath, sortOrder);

        if (item.Type == NotebookItemType.Page)
        {
            if (contentJson.ValueKind != JsonValueKind.Undefined)
            {
                var normalizedContent = tipTapContentService.NormalizePageContent(contentJson, item.Title);
                if (!normalizedContent.Succeeded)
                {
                    return NotesResult<NotebookItemModel>.Failure(
                        normalizedContent.Error!.Kind,
                        normalizedContent.Error.Code,
                        normalizedContent.Error.Message,
                        normalizedContent.Error.Field,
                        normalizedContent.Error.Details);
                }

                item.SetPageContent(
                    NotesSupport.PageContentFormat,
                    normalizedContent.Value!.ContentJson,
                    normalizedContent.Value!.PlainTextContent);
            }
        }

        if (!string.Equals(oldPath, item.Path, StringComparison.Ordinal))
        {
            await UpdateDescendantPathsAsync(notebookId, item.Id, oldPath, item.Path, cancellationToken);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Conflict,
                "notebook_item_conflict",
                "The notebook item changed while the update was being applied.");
        }

        return NotesResult<NotebookItemModel>.Success(NotesSupport.ToItemModel(item));
    }

    public async Task<NotesResult<IReadOnlyList<NotebookItemModel>>> ReorderNotebookItemsAsync(
        Guid notebookId,
        Guid currentUserId,
        IReadOnlyList<ReorderNotebookItemModel> items,
        CancellationToken cancellationToken)
    {
        if (await EnsureOwnedNotebookAsync(notebookId, currentUserId, cancellationToken) is { } accessError)
        {
            return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(accessError.Kind, accessError.Code, accessError.Message);
        }

        var notebookItems = await GetNotebookStructureAsync(notebookId, includeArchived: false, cancellationToken);
        var notebookItemsById = notebookItems.ToDictionary(existingItem => existingItem.Id);
        var idsToLoad = items.Select(item => item.ItemId).ToHashSet();

        foreach (var reorderItem in items)
        {
            if (!notebookItemsById.TryGetValue(reorderItem.ItemId, out var item))
            {
                return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "One or more notebook items were not found.");
            }

            var parent = reorderItem.ParentId is null
                ? null
                : notebookItemsById.GetValueOrDefault(reorderItem.ParentId.Value);
            if (reorderItem.ParentId is not null && parent is null)
            {
                return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.Validation, "invalid_parent", "Parent item was not found in this notebook.");
            }

            if (parent is not null && parent.Type != NotebookItemType.Folder)
            {
                return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.Validation, "invalid_parent", "Parent item must be a folder.");
            }

            if (parent is not null && NotebookItemTree.WouldCreateCycle(notebookItems, item.Id, parent.Id, items))
            {
                return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.Validation, "invalid_parent", "Item cannot be moved into itself or its descendants.");
            }
        }

        try
        {
            var startedTransaction = dbContext.Database.CurrentTransaction is null;
            await using var transaction = startedTransaction
                ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
                : null;

            foreach (var reorderItem in items)
            {
                var item = notebookItemsById[reorderItem.ItemId];
                var oldPath = item.Path;
                var parentPath = reorderItem.ParentId is null
                    ? null
                    : notebookItemsById[reorderItem.ParentId.Value].Path;
                var nextPath = NotebookItemTree.GeneratePath(notebookItems, parentPath, item.Title, item.Id);
                item.UpdateStructure(reorderItem.ParentId, item.Title, nextPath, reorderItem.SortOrder);

                if (!string.Equals(oldPath, item.Path, StringComparison.Ordinal))
                {
                    foreach (var descendantId in NotebookItemTree.GetDescendantIds(notebookItems, item.Id))
                    {
                        idsToLoad.Add(descendantId);
                    }

                    NotebookItemTree.ApplyDescendantPathUpdate(notebookItems, item.Id, oldPath, item.Path);
                }
            }

            var trackedItems = await dbContext.NotebookItems
                .Where(existingItem => existingItem.NotebookId == notebookId && idsToLoad.Contains(existingItem.Id))
                .ToDictionaryAsync(existingItem => existingItem.Id, cancellationToken);

            foreach (var notebookItem in notebookItems.Where(existingItem => idsToLoad.Contains(existingItem.Id)))
            {
                var trackedItem = trackedItems[notebookItem.Id];
                trackedItem.UpdateStructure(notebookItem.ParentId, notebookItem.Title, notebookItem.Path, notebookItem.SortOrder);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(
                NotesFailureKind.Conflict,
                "notebook_item_conflict",
                "One or more notebook items changed while the reorder was being applied.");
        }

        var orderedItems = await dbContext.NotebookItems
            .AsNoTracking()
            .Where(item => item.NotebookId == notebookId && !item.IsArchived)
            .OrderBy(item => item.ParentId)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Title)
            .Select(item => NotesSupport.ToItemModel(item))
            .ToListAsync(cancellationToken);

        return NotesResult<IReadOnlyList<NotebookItemModel>>.Success(orderedItems);
    }

    public async Task<NotesResult> DeleteNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
    {
        if (await EnsureOwnedNotebookAsync(notebookId, currentUserId, cancellationToken) is { } accessError)
        {
            return NotesResult.Failure(accessError.Kind, accessError.Code, accessError.Message);
        }

        var items = await GetNotebookStructureAsync(notebookId, includeArchived: true, cancellationToken);
        var item = items.SingleOrDefault(existingItem => existingItem.Id == itemId);
        if (item is null)
        {
            return NotesResult.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "Notebook item was not found.");
        }

        if (!item.IsArchived)
        {
            return NotesResult.Failure(
                NotesFailureKind.Validation,
                "notebook_item_not_archived",
                "Archive the notebook item before deleting it permanently.");
        }

        var idsToDelete = NotebookItemTree.GetDescendantIds(items, itemId);
        idsToDelete.Add(itemId);
        var entitiesToDelete = await dbContext.NotebookItems
            .Where(existingItem => existingItem.NotebookId == notebookId && idsToDelete.Contains(existingItem.Id))
            .ToListAsync(cancellationToken);
        dbContext.NotebookItems.RemoveRange(entitiesToDelete);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotesResult.Failure(
                NotesFailureKind.Conflict,
                "notebook_item_conflict",
                "The notebook item changed while the delete was being applied.");
        }

        return NotesResult.Success();
    }

    public async Task<NotesResult<NotebookItemModel>> ArchiveNotebookItemAsync(
        Guid notebookId,
        Guid itemId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        if (await EnsureOwnedNotebookAsync(notebookId, currentUserId, cancellationToken) is { } accessError)
        {
            return NotesResult<NotebookItemModel>.Failure(accessError.Kind, accessError.Code, accessError.Message);
        }

        var items = await GetNotebookStructureAsync(notebookId, includeArchived: true, cancellationToken);
        var item = items.SingleOrDefault(existingItem => existingItem.Id == itemId);
        if (item is null)
        {
            return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "Notebook item was not found.");
        }

        if (item.IsArchived)
        {
            return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "notebook_item_archived", "Notebook item is already archived.");
        }

        var idsToArchive = NotebookItemTree.GetDescendantIds(items, itemId);
        idsToArchive.Add(itemId);
        var now = dateTimeProvider.UtcNow;
        var entitiesToArchive = await dbContext.NotebookItems
            .Where(existingItem => existingItem.NotebookId == notebookId && idsToArchive.Contains(existingItem.Id))
            .ToListAsync(cancellationToken);

        foreach (var existingItem in entitiesToArchive)
        {
            existingItem.Archive(now, currentUserId);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Conflict,
                "notebook_item_conflict",
                "The notebook item changed while the archive was being applied.");
        }

        var archivedItem = entitiesToArchive.Single(existingItem => existingItem.Id == itemId);
        return NotesResult<NotebookItemModel>.Success(NotesSupport.ToItemModel(archivedItem));
    }

    public async Task<NotesResult<NotebookItemModel>> RestoreNotebookItemAsync(
        Guid notebookId,
        Guid itemId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        if (await EnsureOwnedNotebookAsync(notebookId, currentUserId, cancellationToken) is { } accessError)
        {
            return NotesResult<NotebookItemModel>.Failure(accessError.Kind, accessError.Code, accessError.Message);
        }

        var items = await GetNotebookStructureAsync(notebookId, includeArchived: true, cancellationToken);
        var item = items.SingleOrDefault(existingItem => existingItem.Id == itemId);
        if (item is null)
        {
            return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "Notebook item was not found.");
        }

        if (!item.IsArchived)
        {
            return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "notebook_item_not_archived", "Notebook item is not archived.");
        }

        if (item.ParentId is Guid parentId)
        {
            var parent = items.SingleOrDefault(existingItem => existingItem.Id == parentId);
            if (parent is null)
            {
                return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "invalid_parent", "Parent item was not found in this notebook.");
            }

            var subtreeIds = NotebookItemTree.GetDescendantIds(items, itemId);
            subtreeIds.Add(itemId);
            if (parent.IsArchived && !subtreeIds.Contains(parent.Id))
            {
                return NotesResult<NotebookItemModel>.Failure(
                    NotesFailureKind.Validation,
                    "parent_archived",
                    "Restore the parent folder before restoring this item.");
            }
        }

        var idsToRestore = NotebookItemTree.GetDescendantIds(items, itemId);
        idsToRestore.Add(itemId);
        var entitiesToRestore = await dbContext.NotebookItems
            .Where(existingItem => existingItem.NotebookId == notebookId && idsToRestore.Contains(existingItem.Id))
            .ToListAsync(cancellationToken);

        foreach (var existingItem in entitiesToRestore)
        {
            existingItem.Restore();
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Conflict,
                "notebook_item_conflict",
                "The notebook item changed while the restore was being applied.");
        }

        var restoredItem = entitiesToRestore.Single(existingItem => existingItem.Id == itemId);
        return NotesResult<NotebookItemModel>.Success(NotesSupport.ToItemModel(restoredItem));
    }

    private async Task<Notebook?> GetOwnedNotebookAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
    {
        return await dbContext.Notebooks.SingleOrDefaultAsync(
            notebook => notebook.Id == notebookId && notebook.OwnerId == currentUserId,
            cancellationToken);
    }

    /// <summary>
    /// Resolves the notebook the caller is allowed to mutate. Returns a non-null
    /// <see cref="NotesError"/> describing why access is denied (forbidden when the
    /// notebook exists but is owned by someone else, not-found when it does not exist),
    /// or null when the caller owns the notebook.
    /// </summary>
    private async Task<NotesError?> EnsureOwnedNotebookAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
    {
        if (await GetOwnedNotebookAsync(notebookId, currentUserId, cancellationToken) is not null)
        {
            return null;
        }

        return await NotebookExistsAsync(notebookId, cancellationToken)
            ? new NotesError(NotesFailureKind.Forbidden, "notebook_forbidden", "Only the notebook owner can modify items.")
            : new NotesError(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
    }

    private async Task<bool> NotebookExistsAsync(Guid notebookId, CancellationToken cancellationToken)
    {
        return await dbContext.Notebooks.AnyAsync(notebook => notebook.Id == notebookId, cancellationToken);
    }

    private async Task<string> GenerateItemPathAsync(
        Guid notebookId,
        string? parentPath,
        string title,
        Guid? currentItemId,
        CancellationToken cancellationToken)
    {
        var baseSlug = NotebookSlugGenerator.FromTitle(title, "page");
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var slug = NotebookSlugGenerator.WithSuffix(baseSlug, attempt);
            var path = string.IsNullOrWhiteSpace(parentPath) ? slug : $"{parentPath}/{slug}";
            var exists = await dbContext.NotebookItems.AnyAsync(
                item => item.NotebookId == notebookId
                    && item.Path == path
                    && item.Id != currentItemId,
                cancellationToken);
            if (!exists)
            {
                return path;
            }
        }

        var finalSlug = $"{baseSlug}-{Guid.NewGuid():N}";
        return string.IsNullOrWhiteSpace(parentPath) ? finalSlug : $"{parentPath}/{finalSlug}";
    }

    private async Task<NotebookItem?> GetParentItemAsync(
        Guid notebookId,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        return parentId is null
            ? null
            : await dbContext.NotebookItems.SingleOrDefaultAsync(
                item => item.NotebookId == notebookId && item.Id == parentId && !item.IsArchived,
                cancellationToken);
    }

    private Task<List<NotebookItem>> GetNotebookStructureAsync(
        Guid notebookId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        return dbContext.NotebookItems
            .AsNoTracking()
            .Where(item => item.NotebookId == notebookId && (includeArchived || !item.IsArchived))
            .Select(item => new NotebookItem
            {
                Id = item.Id,
                NotebookId = item.NotebookId,
                ParentId = item.ParentId,
                Type = item.Type,
                Title = item.Title,
                Slug = item.Slug,
                Path = item.Path,
                SortOrder = item.SortOrder,
                IsArchived = item.IsArchived,
                ArchivedAtUtc = item.ArchivedAtUtc,
                ArchivedByUserId = item.ArchivedByUserId,
                Revision = item.Revision,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    private async Task UpdateDescendantPathsAsync(
        Guid notebookId,
        Guid itemId,
        string oldPath,
        string newPath,
        CancellationToken cancellationToken)
    {
        var descendants = await dbContext.NotebookItems
            .Where(item => item.NotebookId == notebookId && item.Id != itemId && item.Path.StartsWith(oldPath + "/"))
            .ToListAsync(cancellationToken);

        foreach (var descendant in descendants)
        {
            descendant.Path = newPath + descendant.Path[oldPath.Length..];
        }
    }
}
