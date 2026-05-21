using CodeCafe.Application.Common.Interfaces;
using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CodeCafe.Infrastructure.Notes;

public sealed class NotebookCommandService(
    ApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    INotebookQueryService notebookQueryService,
    ITipTapContentService tipTapContentService) : INotebookCommandService
{
    public async Task<NotesResult<NotebookDetailModel>> CreateNotebookAsync(
        Guid currentUserId,
        string title,
        string? description,
        string? visibility,
        CancellationToken cancellationToken)
    {
        if (!NotesSupport.TryParseVisibility(visibility, out var parsedVisibility))
        {
            return NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.Validation, "invalid_visibility", "Visibility must be public, private, or unlisted.");
        }

        var trimmedTitle = title.Trim();
        var now = dateTimeProvider.UtcNow;
        var isPublished = parsedVisibility == NotebookVisibility.Public;
        var notebook = new Notebook
        {
            Id = Guid.NewGuid(),
            OwnerId = currentUserId,
            Title = trimmedTitle,
            Slug = await GenerateNotebookSlugAsync(trimmedTitle, null, cancellationToken),
            Description = NotesSupport.NormalizeOptionalText(description),
            Visibility = parsedVisibility,
            IsPublished = isPublished,
            PublishedAtUtc = isPublished ? now : null
        };

        dbContext.Notebooks.Add(notebook);
        await SaveNotebookWithUniqueSlugRetriesAsync(notebook, trimmedTitle, cancellationToken);

        return await notebookQueryService.GetNotebookByIdAsync(notebook.Id, currentUserId, cancellationToken);
    }

    public async Task<NotesResult<NotebookDetailModel>> UpdateNotebookAsync(
        Guid notebookId,
        Guid currentUserId,
        string title,
        string? description,
        string? visibility,
        CancellationToken cancellationToken)
    {
        if (!NotesSupport.TryParseVisibility(visibility, out var parsedVisibility))
        {
            return NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.Validation, "invalid_visibility", "Visibility must be public, private, or unlisted.");
        }

        var notebook = await GetOwnedNotebookAsync(notebookId, currentUserId, cancellationToken);
        if (notebook is null)
        {
            return await NotebookExistsAsync(notebookId, cancellationToken)
                ? NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.Forbidden, "notebook_forbidden", "Only the notebook owner can modify it.")
                : NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
        }

        var wasPublished = notebook.IsPublished;
        var now = dateTimeProvider.UtcNow;
        var trimmedTitle = title.Trim();
        var titleChanged = !string.Equals(notebook.Title, trimmedTitle, StringComparison.Ordinal);
        notebook.Title = trimmedTitle;
        if (titleChanged)
        {
            notebook.Slug = await GenerateNotebookSlugAsync(trimmedTitle, notebook.Id, cancellationToken);
        }

        notebook.Description = NotesSupport.NormalizeOptionalText(description);
        notebook.Visibility = parsedVisibility;
        notebook.IsPublished = parsedVisibility == NotebookVisibility.Public;
        notebook.PublishedAtUtc = parsedVisibility == NotebookVisibility.Public
            ? notebook.PublishedAtUtc ?? now
            : null;

        if (!wasPublished && notebook.IsPublished)
        {
            notebook.PublishedAtUtc = now;
        }

        await SaveNotebookWithUniqueSlugRetriesAsync(notebook, trimmedTitle, cancellationToken);

        return await notebookQueryService.GetNotebookByIdAsync(notebook.Id, currentUserId, cancellationToken);
    }

    public async Task<NotesResult> DeleteNotebookAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
    {
        var notebook = await GetOwnedNotebookAsync(notebookId, currentUserId, cancellationToken);
        if (notebook is null)
        {
            return await NotebookExistsAsync(notebookId, cancellationToken)
                ? NotesResult.Failure(NotesFailureKind.Forbidden, "notebook_forbidden", "Only the notebook owner can delete it.")
                : NotesResult.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
        }

        dbContext.Notebooks.Remove(notebook);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NotesResult.Success();
    }

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
        var notebook = await GetOwnedNotebookAsync(notebookId, currentUserId, cancellationToken);
        if (notebook is null)
        {
            return await NotebookExistsAsync(notebookId, cancellationToken)
                ? NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Forbidden, "notebook_forbidden", "Only the notebook owner can modify items.")
                : NotesResult<NotebookItemModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
        }

        if (!NotesSupport.TryParseItemType(type, out var itemType))
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
            normalizedContent = tipTapContentService.NormalizePageContent(contentJson);
            if (!normalizedContent.Succeeded)
            {
                return NotesResult<NotebookItemModel>.Failure(
                    normalizedContent.Error!.Kind,
                    normalizedContent.Error.Code,
                    normalizedContent.Error.Message);
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
        CancellationToken cancellationToken)
    {
        var notebook = await GetOwnedNotebookAsync(notebookId, currentUserId, cancellationToken);
        if (notebook is null)
        {
            return await NotebookExistsAsync(notebookId, cancellationToken)
                ? NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Forbidden, "notebook_forbidden", "Only the notebook owner can modify items.")
                : NotesResult<NotebookItemModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
        }

        var item = await dbContext.NotebookItems.SingleOrDefaultAsync(
            existingItem => existingItem.NotebookId == notebookId && existingItem.Id == itemId,
            cancellationToken);
        if (item is null)
        {
            return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "Notebook item was not found.");
        }

        var notebookItems = await dbContext.NotebookItems
            .Where(existingItem => existingItem.NotebookId == notebookId)
            .ToListAsync(cancellationToken);
        var requestedParentId = item.ParentId;
        var currentParent = NotesSupport.ValidateRequestedParent(notebookItems, item.Id, item.ParentId);
        var nextParent = currentParent;

        if (parentId.ValueKind != JsonValueKind.Undefined)
        {
            if (!NotesSupport.TryParseOptionalGuid(parentId, out requestedParentId))
            {
                return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "invalid_parent", "ParentId must be a GUID or null.");
            }

            nextParent = NotesSupport.ValidateRequestedParent(notebookItems, item.Id, requestedParentId);
            if (requestedParentId is not null && nextParent is null)
            {
                return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "invalid_parent", "Parent item was not found in this notebook.");
            }

            if (nextParent is not null && nextParent.Type != NotebookItemType.Folder)
            {
                return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "invalid_parent", "Parent item must be a folder.");
            }

            if (nextParent is not null && NotesSupport.WouldCreateCycle(notebookItems, item.Id, nextParent.Id))
            {
                return NotesResult<NotebookItemModel>.Failure(NotesFailureKind.Validation, "invalid_parent", "Item cannot be moved into itself or its descendants.");
            }
        }

        var oldPath = item.Path;
        var parentPath = nextParent?.Path;
        item.ParentId = requestedParentId;
        item.Title = title.Trim();
        item.Path = NotesSupport.GenerateItemPath(notebookItems, parentPath, item.Title, item.Id);
        item.Slug = item.Path.Split('/')[^1];
        if (sortOrder.HasValue)
        {
            item.SortOrder = sortOrder.Value;
        }

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
                        normalizedContent.Error.Message);
                }

                item.ContentFormat = NotesSupport.PageContentFormat;
                item.ContentJson = normalizedContent.Value!.ContentJson;
                item.PlainTextContent = normalizedContent.Value!.PlainTextContent;
            }
        }

        if (!string.Equals(oldPath, item.Path, StringComparison.Ordinal))
        {
            await UpdateDescendantPathsAsync(notebookId, item.Id, oldPath, item.Path, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return NotesResult<NotebookItemModel>.Success(NotesSupport.ToItemModel(item));
    }

    public async Task<NotesResult<IReadOnlyList<NotebookItemModel>>> ReorderNotebookItemsAsync(
        Guid notebookId,
        Guid currentUserId,
        IReadOnlyList<ReorderNotebookItemModel> items,
        CancellationToken cancellationToken)
    {
        var notebook = await GetOwnedNotebookAsync(notebookId, currentUserId, cancellationToken);
        if (notebook is null)
        {
            return await NotebookExistsAsync(notebookId, cancellationToken)
                ? NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.Forbidden, "notebook_forbidden", "Only the notebook owner can modify items.")
                : NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
        }

        var notebookItems = await dbContext.NotebookItems
            .Where(existingItem => existingItem.NotebookId == notebookId)
            .ToListAsync(cancellationToken);
        var notebookItemsById = notebookItems.ToDictionary(existingItem => existingItem.Id);

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

            if (parent is not null && NotesSupport.WouldCreateCycle(notebookItems, item.Id, parent.Id, items))
            {
                return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.Validation, "invalid_parent", "Item cannot be moved into itself or its descendants.");
            }
        }

        foreach (var reorderItem in items)
        {
            var item = notebookItemsById[reorderItem.ItemId];
            var oldPath = item.Path;
            item.ParentId = reorderItem.ParentId;
            item.SortOrder = reorderItem.SortOrder;

            var parentPath = reorderItem.ParentId is null
                ? null
                : notebookItemsById[reorderItem.ParentId.Value].Path;
            item.Path = NotesSupport.GenerateItemPath(notebookItems, parentPath, item.Title, item.Id);
            item.Slug = item.Path.Split('/')[^1];

            if (!string.Equals(oldPath, item.Path, StringComparison.Ordinal))
            {
                NotesSupport.ApplyDescendantPathUpdate(notebookItems, item.Id, oldPath, item.Path);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var orderedItems = notebookItems
            .OrderBy(item => item.ParentId)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Title)
            .Select(NotesSupport.ToItemModel)
            .ToList();

        return NotesResult<IReadOnlyList<NotebookItemModel>>.Success(orderedItems);
    }

    public async Task<NotesResult> DeleteNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
    {
        var notebook = await GetOwnedNotebookAsync(notebookId, currentUserId, cancellationToken);
        if (notebook is null)
        {
            return await NotebookExistsAsync(notebookId, cancellationToken)
                ? NotesResult.Failure(NotesFailureKind.Forbidden, "notebook_forbidden", "Only the notebook owner can modify items.")
                : NotesResult.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
        }

        var items = await dbContext.NotebookItems
            .Where(item => item.NotebookId == notebookId)
            .ToListAsync(cancellationToken);
        var item = items.SingleOrDefault(existingItem => existingItem.Id == itemId);
        if (item is null)
        {
            return NotesResult.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "Notebook item was not found.");
        }

        var idsToDelete = NotesSupport.GetDescendantIds(items, itemId);
        idsToDelete.Add(itemId);
        dbContext.NotebookItems.RemoveRange(items.Where(existingItem => idsToDelete.Contains(existingItem.Id)));
        await dbContext.SaveChangesAsync(cancellationToken);

        return NotesResult.Success();
    }

    private async Task<Notebook?> GetOwnedNotebookAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
    {
        return await dbContext.Notebooks.SingleOrDefaultAsync(
            notebook => notebook.Id == notebookId && notebook.OwnerId == currentUserId,
            cancellationToken);
    }

    private async Task<bool> NotebookExistsAsync(Guid notebookId, CancellationToken cancellationToken)
    {
        return await dbContext.Notebooks.AnyAsync(notebook => notebook.Id == notebookId, cancellationToken);
    }

    private async Task<string> GenerateNotebookSlugAsync(
        string title,
        Guid? currentNotebookId,
        CancellationToken cancellationToken)
    {
        var baseSlug = SlugGenerator.FromTitle(title, "note");
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var slug = SlugGenerator.WithSuffix(baseSlug, attempt);
            var exists = await dbContext.Notebooks.AnyAsync(
                notebook => notebook.Slug == slug && notebook.Id != currentNotebookId,
                cancellationToken);
            if (!exists)
            {
                return slug;
            }
        }

        return $"{baseSlug}-{Guid.NewGuid():N}"[..Math.Min(baseSlug.Length + 33, 180)];
    }

    private async Task SaveNotebookWithUniqueSlugRetriesAsync(
        Notebook notebook,
        string title,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException exception) when (NotesSupport.IsDuplicateNotebookSlugException(exception) && attempt < 4)
            {
                notebook.Slug = await GenerateNotebookSlugAsync(title, notebook.Id, cancellationToken);
                if (dbContext.Entry(notebook).State == EntityState.Modified)
                {
                    dbContext.Entry(notebook).Property(existingNotebook => existingNotebook.Slug).IsModified = true;
                }
            }
        }
    }

    private async Task<string> GenerateItemPathAsync(
        Guid notebookId,
        string? parentPath,
        string title,
        Guid? currentItemId,
        CancellationToken cancellationToken)
    {
        var baseSlug = SlugGenerator.FromTitle(title, "page");
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var slug = SlugGenerator.WithSuffix(baseSlug, attempt);
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
                item => item.NotebookId == notebookId && item.Id == parentId,
                cancellationToken);
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
