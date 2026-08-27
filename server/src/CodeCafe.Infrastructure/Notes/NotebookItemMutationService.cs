using System.Text.Json;
using CodeCafe.Application.Common;
using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Notes;

public sealed class NotebookItemMutationService(
    ApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    ITipTapContentService tipTapContentService
) : INotebookItemMutationService
{
    public async Task<NotesResult<NotebookItemModel>> CreateNotebookItemAsync(
        Guid notebookId,
        Guid currentUserId,
        Guid? parentId,
        string type,
        string title,
        int sortOrder,
        JsonElement? contentJson,
        CancellationToken cancellationToken
    )
    {
        if (
            await EnsureOwnedNotebookAsync(notebookId, currentUserId, cancellationToken) is
            { } accessError
        )
        {
            return NotesResult<NotebookItemModel>.Failure(
                accessError.Kind,
                accessError.Code,
                accessError.Message
            );
        }

        if (!NotebookInput.TryParseItemType(type, out var itemType))
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Validation,
                "invalid_item_type",
                "Item type must be folder or page."
            );
        }

        var parent = await GetParentItemAsync(notebookId, parentId, cancellationToken);
        if (NotebookItemTree.ValidateParentCandidate(parent, parentId) is { } parentViolation)
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Validation,
                "invalid_parent",
                ToParentViolationMessage(parentViolation)
            );
        }

        if (!NotebookItemPath.HasRoomForChild(parent?.Path))
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Validation,
                "path_too_long",
                "The parent folder is nested too deeply to hold another item. Move it higher in the tree or shorten the folder names."
            );
        }

        var trimmedTitle = title.Trim();
        var path = await GenerateItemPathAsync(
            notebookId,
            parent?.Path,
            trimmedTitle,
            null,
            cancellationToken
        );
        var normalizedContent = NotesResult<TipTapContentModel>.Success(
            new TipTapContentModel(null, null)
        );
        if (itemType == NotebookItemType.Page)
        {
            normalizedContent = tipTapContentService.NormalizePageContent(contentJson);
            if (!normalizedContent.Succeeded)
            {
                return NotesResult<NotebookItemModel>.Failure(
                    normalizedContent.Error!.Kind,
                    normalizedContent.Error.Code,
                    normalizedContent.Error.Message,
                    normalizedContent.Error.Field,
                    normalizedContent.Error.Details
                );
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
            ContentFormat =
                itemType == NotebookItemType.Page ? NotesSupport.PageContentFormat : null,
            ContentJson =
                itemType == NotebookItemType.Page ? normalizedContent.Value!.ContentJson : null,
            PlainTextContent =
                itemType == NotebookItemType.Page
                    ? normalizedContent.Value!.PlainTextContent
                    : null,
        };

        dbContext.NotebookItems.Add(item);

        // (NotebookId, Path) is unique and the path was picked by a check-then-insert, so two
        // concurrent creates with the same title race. Regenerate against the now-committed row
        // and retry, mirroring the notebook-slug retry in NotebookMutationStore.SaveNotebookAsync.
        const int maxAttempts = 5;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                break;
            }
            catch (DbUpdateException exception)
                when (NotesSupport.IsDuplicateItemPathException(exception))
            {
                if (attempt >= maxAttempts)
                {
                    return NotesResult<NotebookItemModel>.Failure(
                        NotesFailureKind.Conflict,
                        "notebook_item_conflict",
                        "Another item with the same name was created at the same time. Try again."
                    );
                }

                var retryPath = await GenerateItemPathAsync(
                    notebookId,
                    parent?.Path,
                    trimmedTitle,
                    item.Id,
                    cancellationToken
                );
                item.Path = retryPath;
                item.Slug = retryPath.Split('/')[^1];
            }
        }

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
        DateTimeOffset? expectedUpdatedAtUtc = null
    )
    {
        if (
            await EnsureOwnedNotebookAsync(notebookId, currentUserId, cancellationToken) is
            { } accessError
        )
        {
            return NotesResult<NotebookItemModel>.Failure(
                accessError.Kind,
                accessError.Code,
                accessError.Message
            );
        }

        var item = await dbContext.NotebookItems.SingleOrDefaultAsync(
            existingItem =>
                existingItem.NotebookId == notebookId
                && existingItem.Id == itemId
                && !existingItem.IsArchived,
            cancellationToken
        );
        if (item is null)
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_item_not_found",
                "Notebook item was not found."
            );
        }

        if (expectedUpdatedAtUtc.HasValue && item.UpdatedAtUtc != expectedUpdatedAtUtc.Value)
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Conflict,
                "content_conflict",
                "The page changed after the expected timestamp."
            );
        }

        var notebookItems = await GetNotebookStructureAsync(
            notebookId,
            includeArchived: false,
            cancellationToken
        );
        var requestedParentId = item.ParentId;
        var currentParent = NotebookItemTree.FindRequestedParent(
            notebookItems,
            item.Id,
            item.ParentId
        );
        var nextParent = currentParent;

        if (parentId.ValueKind != JsonValueKind.Undefined)
        {
            if (!NotebookInput.TryParseOptionalGuid(parentId, out requestedParentId))
            {
                return NotesResult<NotebookItemModel>.Failure(
                    NotesFailureKind.Validation,
                    "invalid_parent",
                    "ParentId must be a GUID or null."
                );
            }

            nextParent = NotebookItemTree.FindRequestedParent(
                notebookItems,
                item.Id,
                requestedParentId
            );
            if (
                NotebookItemTree.ValidateParentCandidate(nextParent, requestedParentId) is
                { } parentViolation
            )
            {
                return NotesResult<NotebookItemModel>.Failure(
                    NotesFailureKind.Validation,
                    "invalid_parent",
                    ToParentViolationMessage(parentViolation)
                );
            }

            if (
                nextParent is not null
                && NotebookItemTree.WouldCreateCycle(notebookItems, item.Id, nextParent.Id)
            )
            {
                return NotesResult<NotebookItemModel>.Failure(
                    NotesFailureKind.Validation,
                    "invalid_parent",
                    "Item cannot be moved into itself or its descendants."
                );
            }
        }

        var oldPath = item.Path;
        var parentPath = nextParent?.Path;
        if (!NotebookItemPath.HasRoomForChild(parentPath))
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Validation,
                "path_too_long",
                "The target folder is nested too deeply to hold this item. Move it higher in the tree or shorten the folder names."
            );
        }

        var trimmedTitle = title.Trim();
        var nextPath = NotebookItemTree.GeneratePath(
            notebookItems,
            parentPath,
            trimmedTitle,
            item.Id
        );

        // Renaming or moving a folder rewrites every descendant path; reject the request when that
        // rewrite would push a descendant past the column budget instead of failing mid-save.
        if (!NotebookItemPath.DescendantsFitAfterMove(notebookItems, item.Id, oldPath, nextPath))
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Validation,
                "path_too_long",
                "This change would make the paths of nested items too long. Use a shorter name or move the item higher in the tree."
            );
        }

        item.UpdateStructure(requestedParentId, trimmedTitle, nextPath, sortOrder);

        if (item.Type == NotebookItemType.Page)
        {
            if (contentJson.ValueKind != JsonValueKind.Undefined)
            {
                var normalizedContent = tipTapContentService.NormalizePageContent(contentJson);
                if (!normalizedContent.Succeeded)
                {
                    return NotesResult<NotebookItemModel>.Failure(
                        normalizedContent.Error!.Kind,
                        normalizedContent.Error.Code,
                        normalizedContent.Error.Message,
                        normalizedContent.Error.Field,
                        normalizedContent.Error.Details
                    );
                }

                item.SetPageContent(
                    NotesSupport.PageContentFormat,
                    normalizedContent.Value!.ContentJson,
                    normalizedContent.Value!.PlainTextContent
                );
            }
        }

        if (!string.Equals(oldPath, item.Path, StringComparison.Ordinal))
        {
            await UpdateDescendantPathsAsync(
                notebookId,
                item.Id,
                oldPath,
                item.Path,
                cancellationToken
            );
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
                "The notebook item changed while the update was being applied."
            );
        }
        catch (DbUpdateException exception)
            when (NotesSupport.IsDuplicateItemPathException(exception))
        {
            // A concurrent create/rename took this path between the uniqueness check and the save.
            // Retrying is not safe here because descendant paths were already rewritten in-context,
            // so surface the conflict and let the caller refresh.
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Conflict,
                "notebook_item_conflict",
                "Another item with the same name was created at the same time. Refresh and try again."
            );
        }

        return NotesResult<NotebookItemModel>.Success(NotesSupport.ToItemModel(item));
    }

    public async Task<NotesResult<IReadOnlyList<NotebookItemModel>>> ReorderNotebookItemsAsync(
        Guid notebookId,
        Guid currentUserId,
        IReadOnlyList<ReorderNotebookItemModel> items,
        CancellationToken cancellationToken
    )
    {
        if (
            await EnsureOwnedNotebookAsync(notebookId, currentUserId, cancellationToken) is
            { } accessError
        )
        {
            return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(
                accessError.Kind,
                accessError.Code,
                accessError.Message
            );
        }

        var notebookItems = await GetNotebookStructureAsync(
            notebookId,
            includeArchived: false,
            cancellationToken
        );
        var notebookItemsById = notebookItems.ToDictionary(existingItem => existingItem.Id);
        var idsToLoad = items.Select(item => item.ItemId).ToHashSet();
        var pendingParentOverrides = items.Select(item => (item.ItemId, item.ParentId)).ToList();

        foreach (var reorderItem in items)
        {
            if (!notebookItemsById.TryGetValue(reorderItem.ItemId, out var item))
            {
                return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(
                    NotesFailureKind.NotFound,
                    "notebook_item_not_found",
                    "One or more notebook items were not found."
                );
            }

            var parent = reorderItem.ParentId is null
                ? null
                : notebookItemsById.GetValueOrDefault(reorderItem.ParentId.Value);
            if (
                NotebookItemTree.ValidateParentCandidate(parent, reorderItem.ParentId) is
                { } parentViolation
            )
            {
                return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(
                    NotesFailureKind.Validation,
                    "invalid_parent",
                    ToParentViolationMessage(parentViolation)
                );
            }

            if (
                parent is not null
                && NotebookItemTree.WouldCreateCycle(
                    notebookItems,
                    item.Id,
                    parent.Id,
                    pendingParentOverrides
                )
            )
            {
                return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(
                    NotesFailureKind.Validation,
                    "invalid_parent",
                    "Item cannot be moved into itself or its descendants."
                );
            }

            if (!NotebookItemPath.HasRoomForChild(parent?.Path))
            {
                return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(
                    NotesFailureKind.Validation,
                    "path_too_long",
                    "The target folder is nested too deeply to hold this item. Move it higher in the tree or shorten the folder names."
                );
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
                var nextPath = NotebookItemTree.GeneratePath(
                    notebookItems,
                    parentPath,
                    item.Title,
                    item.Id
                );
                if (
                    !NotebookItemPath.DescendantsFitAfterMove(
                        notebookItems,
                        item.Id,
                        oldPath,
                        nextPath
                    )
                )
                {
                    return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(
                        NotesFailureKind.Validation,
                        "path_too_long",
                        "This move would make the paths of nested items too long. Move the item higher in the tree or shorten the folder names."
                    );
                }

                item.UpdateStructure(
                    reorderItem.ParentId,
                    item.Title,
                    nextPath,
                    reorderItem.SortOrder
                );

                if (!string.Equals(oldPath, item.Path, StringComparison.Ordinal))
                {
                    foreach (
                        var descendantId in NotebookItemTree.GetDescendantIds(
                            notebookItems,
                            item.Id
                        )
                    )
                    {
                        idsToLoad.Add(descendantId);
                    }

                    NotebookItemTree.ApplyDescendantPathUpdate(
                        notebookItems,
                        item.Id,
                        oldPath,
                        item.Path
                    );
                }
            }

            var trackedItems = await dbContext
                .NotebookItems.Where(existingItem =>
                    existingItem.NotebookId == notebookId && idsToLoad.Contains(existingItem.Id)
                )
                .ToDictionaryAsync(existingItem => existingItem.Id, cancellationToken);

            foreach (
                var notebookItem in notebookItems.Where(existingItem =>
                    idsToLoad.Contains(existingItem.Id)
                )
            )
            {
                var trackedItem = trackedItems[notebookItem.Id];
                trackedItem.UpdateStructure(
                    notebookItem.ParentId,
                    notebookItem.Title,
                    notebookItem.Path,
                    notebookItem.SortOrder
                );
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
                "One or more notebook items changed while the reorder was being applied."
            );
        }
        catch (DbUpdateException exception)
            when (NotesSupport.IsDuplicateItemPathException(exception))
        {
            return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(
                NotesFailureKind.Conflict,
                "notebook_item_conflict",
                "Another item took the same path while the reorder was being applied. Refresh and try again."
            );
        }

        var orderedItems = await dbContext
            .NotebookItems.AsNoTracking()
            .Where(item => item.NotebookId == notebookId && !item.IsArchived)
            .OrderBy(item => item.ParentId)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Title)
            .Select(item => NotesSupport.ToItemModel(item))
            .ToListAsync(cancellationToken);

        return NotesResult<IReadOnlyList<NotebookItemModel>>.Success(orderedItems);
    }

    public async Task<NotesResult> DeleteNotebookItemAsync(
        Guid notebookId,
        Guid itemId,
        Guid currentUserId,
        CancellationToken cancellationToken
    )
    {
        if (
            await EnsureOwnedNotebookAsync(notebookId, currentUserId, cancellationToken) is
            { } accessError
        )
        {
            return NotesResult.Failure(accessError.Kind, accessError.Code, accessError.Message);
        }

        var items = await GetNotebookStructureAsync(
            notebookId,
            includeArchived: true,
            cancellationToken
        );
        var item = items.SingleOrDefault(existingItem => existingItem.Id == itemId);
        if (item is null)
        {
            return NotesResult.Failure(
                NotesFailureKind.NotFound,
                "notebook_item_not_found",
                "Notebook item was not found."
            );
        }

        if (!item.IsArchived)
        {
            return NotesResult.Failure(
                NotesFailureKind.Validation,
                "notebook_item_not_archived",
                "Archive the notebook item before deleting it permanently."
            );
        }

        var idsToDelete = NotebookItemTree.GetDescendantIds(items, itemId);
        idsToDelete.Add(itemId);
        var entitiesToDelete = await dbContext
            .NotebookItems.Where(existingItem =>
                existingItem.NotebookId == notebookId && idsToDelete.Contains(existingItem.Id)
            )
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
                "The notebook item changed while the delete was being applied."
            );
        }

        return NotesResult.Success();
    }

    public async Task<NotesResult<NotebookItemModel>> ArchiveNotebookItemAsync(
        Guid notebookId,
        Guid itemId,
        Guid currentUserId,
        CancellationToken cancellationToken
    )
    {
        if (
            await EnsureOwnedNotebookAsync(notebookId, currentUserId, cancellationToken) is
            { } accessError
        )
        {
            return NotesResult<NotebookItemModel>.Failure(
                accessError.Kind,
                accessError.Code,
                accessError.Message
            );
        }

        var items = await GetNotebookStructureAsync(
            notebookId,
            includeArchived: true,
            cancellationToken
        );
        var item = items.SingleOrDefault(existingItem => existingItem.Id == itemId);
        if (item is null)
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_item_not_found",
                "Notebook item was not found."
            );
        }

        if (item.IsArchived)
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Validation,
                "notebook_item_archived",
                "Notebook item is already archived."
            );
        }

        var idsToArchive = NotebookItemTree.GetDescendantIds(items, itemId);
        idsToArchive.Add(itemId);
        var now = dateTimeProvider.UtcNow;
        var entitiesToArchive = await dbContext
            .NotebookItems.Where(existingItem =>
                existingItem.NotebookId == notebookId && idsToArchive.Contains(existingItem.Id)
            )
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
                "The notebook item changed while the archive was being applied."
            );
        }

        var archivedItem = entitiesToArchive.Single(existingItem => existingItem.Id == itemId);
        return NotesResult<NotebookItemModel>.Success(NotesSupport.ToItemModel(archivedItem));
    }

    public async Task<NotesResult<NotebookItemModel>> RestoreNotebookItemAsync(
        Guid notebookId,
        Guid itemId,
        Guid currentUserId,
        CancellationToken cancellationToken
    )
    {
        if (
            await EnsureOwnedNotebookAsync(notebookId, currentUserId, cancellationToken) is
            { } accessError
        )
        {
            return NotesResult<NotebookItemModel>.Failure(
                accessError.Kind,
                accessError.Code,
                accessError.Message
            );
        }

        var items = await GetNotebookStructureAsync(
            notebookId,
            includeArchived: true,
            cancellationToken
        );
        var item = items.SingleOrDefault(existingItem => existingItem.Id == itemId);
        if (item is null)
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_item_not_found",
                "Notebook item was not found."
            );
        }

        if (NotebookItemTree.ValidateRestore(items, item) is { } restoreViolation)
        {
            return ToRestoreFailure(restoreViolation);
        }

        var idsToRestore = NotebookItemTree.GetDescendantIds(items, itemId);
        idsToRestore.Add(itemId);
        var entitiesToRestore = await dbContext
            .NotebookItems.Where(existingItem =>
                existingItem.NotebookId == notebookId && idsToRestore.Contains(existingItem.Id)
            )
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
                "The notebook item changed while the restore was being applied."
            );
        }

        var restoredItem = entitiesToRestore.Single(existingItem => existingItem.Id == itemId);
        return NotesResult<NotebookItemModel>.Success(NotesSupport.ToItemModel(restoredItem));
    }

    private async Task<Notebook?> GetOwnedNotebookAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken
    )
    {
        return await dbContext.Notebooks.SingleOrDefaultAsync(
            notebook => notebook.Id == notebookId && notebook.OwnerId == currentUserId,
            cancellationToken
        );
    }

    /// <summary>
    /// Resolves the notebook the caller is allowed to mutate. Returns a non-null
    /// <see cref="NotesError"/> describing why access is denied (forbidden when the
    /// notebook exists but is owned by someone else, not-found when it does not exist),
    /// or null when the caller owns the notebook.
    /// </summary>
    private async Task<NotesError?> EnsureOwnedNotebookAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken
    )
    {
        if (await GetOwnedNotebookAsync(notebookId, currentUserId, cancellationToken) is not null)
        {
            return null;
        }

        return await NotebookExistsAsync(notebookId, cancellationToken)
            ? new NotesError(
                NotesFailureKind.Forbidden,
                "notebook_forbidden",
                "Only the notebook owner can modify items."
            )
            : new NotesError(
                NotesFailureKind.NotFound,
                "notebook_not_found",
                "Notebook was not found."
            );
    }

    private async Task<bool> NotebookExistsAsync(
        Guid notebookId,
        CancellationToken cancellationToken
    )
    {
        return await dbContext.Notebooks.AnyAsync(
            notebook => notebook.Id == notebookId,
            cancellationToken
        );
    }

    /// <summary>
    /// Generates a path that is unique within the notebook. The slug is budgeted against the
    /// remaining path space (see <see cref="NotebookItemPath"/>) so long titles and deep nesting
    /// cannot overflow the Slug/Path columns. Callers must have verified
    /// <see cref="NotebookItemPath.HasRoomForChild"/> for the parent.
    /// </summary>
    private async Task<string> GenerateItemPathAsync(
        Guid notebookId,
        string? parentPath,
        string title,
        Guid? currentItemId,
        CancellationToken cancellationToken
    )
    {
        var slugBudget = NotebookItemPath.GetSlugBudget(parentPath);
        var baseSlug = NotebookSlugGenerator.FromTitle(title, "page", slugBudget);
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var slug = NotebookSlugGenerator.WithSuffix(baseSlug, attempt, slugBudget);
            var path = string.IsNullOrWhiteSpace(parentPath) ? slug : $"{parentPath}/{slug}";
            var exists = await dbContext.NotebookItems.AnyAsync(
                item =>
                    item.NotebookId == notebookId && item.Path == path && item.Id != currentItemId,
                cancellationToken
            );
            if (!exists)
            {
                return path;
            }
        }

        var finalSlug = NotebookSlugGenerator.WithUniqueSuffix(baseSlug, slugBudget);
        return string.IsNullOrWhiteSpace(parentPath) ? finalSlug : $"{parentPath}/{finalSlug}";
    }

    private async Task<NotebookItem?> GetParentItemAsync(
        Guid notebookId,
        Guid? parentId,
        CancellationToken cancellationToken
    )
    {
        return parentId is null
            ? null
            : await dbContext.NotebookItems.SingleOrDefaultAsync(
                item => item.NotebookId == notebookId && item.Id == parentId && !item.IsArchived,
                cancellationToken
            );
    }

    private Task<List<NotebookItem>> GetNotebookStructureAsync(
        Guid notebookId,
        bool includeArchived,
        CancellationToken cancellationToken
    )
    {
        return dbContext
            .NotebookItems.AsNoTracking()
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
                UpdatedAtUtc = item.UpdatedAtUtc,
            })
            .ToListAsync(cancellationToken);
    }

    private async Task UpdateDescendantPathsAsync(
        Guid notebookId,
        Guid itemId,
        string oldPath,
        string newPath,
        CancellationToken cancellationToken
    )
    {
        var descendants = await dbContext
            .NotebookItems.Where(item =>
                item.NotebookId == notebookId
                && item.Id != itemId
                && item.Path.StartsWith(oldPath + "/")
            )
            .ToListAsync(cancellationToken);

        foreach (var descendant in descendants)
        {
            descendant.Path = newPath + descendant.Path[oldPath.Length..];
        }
    }

    private static string ToParentViolationMessage(NotebookItemParentViolation violation)
    {
        return violation == NotebookItemParentViolation.NotFound
            ? "Parent item was not found in this notebook."
            : "Parent item must be a folder.";
    }

    private static NotesResult<NotebookItemModel> ToRestoreFailure(
        NotebookItemRestoreViolation violation
    )
    {
        return violation switch
        {
            NotebookItemRestoreViolation.NotArchived => NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Validation,
                "notebook_item_not_archived",
                "Notebook item is not archived."
            ),
            NotebookItemRestoreViolation.ParentNotFound => NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Validation,
                "invalid_parent",
                "Parent item was not found in this notebook."
            ),
            _ => NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Validation,
                "parent_archived",
                "Restore the parent folder before restoring this item."
            ),
        };
    }
}
