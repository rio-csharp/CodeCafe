using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Modules.Notes.Domain.Notes;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Modules.Notes.Infrastructure.Notes;

public sealed partial class NotebookReadService
{
    public async Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetPublicNotebookItemsAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var notebook = await GetPublicNotebookEntityBySlugAsync(slug, cancellationToken);
        if (notebook is null)
        {
            return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(
                NotesFailureKind.NotFound,
                "notebook_not_found",
                "Notebook was not found.");
        }

        var items = await dbContext.NotebookItems
            .AsNoTracking()
            .Where(item => item.NotebookId == notebook.Id && !item.IsArchived)
            .OrderBy(item => item.ParentId)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Title)
            .Select(item => NotesSupport.ToItemModel(item))
            .ToListAsync(cancellationToken);

        return NotesResult<IReadOnlyList<NotebookItemModel>>.Success(items);
    }

    public async Task<NotesResult<NotebookItemModel>> GetPublicNotebookItemAsync(
        string slug,
        string path,
        CancellationToken cancellationToken)
    {
        var notebook = await GetPublicNotebookEntityBySlugAsync(slug, cancellationToken);
        if (notebook is null)
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_not_found",
                "Notebook was not found.");
        }

        var normalizedPath = NotebookInput.NormalizePath(path);
        var item = await dbContext.NotebookItems
            .AsNoTracking()
            .SingleOrDefaultAsync(
                existingItem => existingItem.NotebookId == notebook.Id && existingItem.Path == normalizedPath && !existingItem.IsArchived,
                cancellationToken);

        return item is null
            ? NotesResult<NotebookItemModel>.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "Notebook item was not found.")
            : NotesResult<NotebookItemModel>.Success(NotesSupport.ToItemModel(item));
    }

    public async Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetNotebookItemsAsync(
        Guid notebookId,
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeContent = true,
        int? limit = null)
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(notebookId, currentUserId, cancellationToken);
        if (!notebookAccessResult.Succeeded)
        {
            return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(
                notebookAccessResult.Error!.Kind,
                notebookAccessResult.Error.Code,
                notebookAccessResult.Error.Message);
        }

        if (GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is { } archiveError)
        {
            return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(
                archiveError.Kind,
                archiveError.Code,
                archiveError.Message);
        }

        var rows = await BuildItemRowQuery(
                ApplyLimit(
                    OrderNotebookItems(BuildNotebookItemsQuery(notebookId, search, includeArchived)),
                    limit),
                includeContent)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => NotesSupport.ToItemModel(row, includeContent))
            .ToList();

        return NotesResult<IReadOnlyList<NotebookItemModel>>.Success(items);
    }

    public async Task<NotesResult<NotebookItemModel>> GetNotebookItemByIdAsync(
        Guid notebookId,
        Guid itemId,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(notebookId, currentUserId, cancellationToken);
        if (!notebookAccessResult.Succeeded)
        {
            return NotesResult<NotebookItemModel>.Failure(
                notebookAccessResult.Error!.Kind,
                notebookAccessResult.Error.Code,
                notebookAccessResult.Error.Message);
        }

        if (GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is { } archiveError)
        {
            return NotesResult<NotebookItemModel>.Failure(
                archiveError.Kind,
                archiveError.Code,
                archiveError.Message);
        }

        var item = await dbContext.NotebookItems
            .AsNoTracking()
            .SingleOrDefaultAsync(
                existingItem =>
                    existingItem.NotebookId == notebookAccessResult.Value!.Id
                    && existingItem.Id == itemId
                    && (includeArchived || !existingItem.IsArchived),
                cancellationToken);

        return item is null
            ? NotesResult<NotebookItemModel>.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "Notebook item was not found.")
            : NotesResult<NotebookItemModel>.Success(NotesSupport.ToItemModel(item));
    }

    public async Task<NotesResult<NotebookItemModel>> GetNotebookItemByPathAsync(
        string notebookSlug,
        string path,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(notebookSlug, currentUserId, cancellationToken);
        if (!notebookAccessResult.Succeeded)
        {
            return NotesResult<NotebookItemModel>.Failure(
                notebookAccessResult.Error!.Kind,
                notebookAccessResult.Error.Code,
                notebookAccessResult.Error.Message);
        }

        if (GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is { } archiveError)
        {
            return NotesResult<NotebookItemModel>.Failure(
                archiveError.Kind,
                archiveError.Code,
                archiveError.Message);
        }

        var normalizedPath = NotebookInput.NormalizePath(path);
        var item = await dbContext.NotebookItems
            .AsNoTracking()
            .SingleOrDefaultAsync(existingItem =>
                    existingItem.NotebookId == notebookAccessResult.Value!.Id
                    && existingItem.Path == normalizedPath
                    && (includeArchived || !existingItem.IsArchived),
                cancellationToken);

        return item is null
            ? NotesResult<NotebookItemModel>.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "Notebook item was not found.")
            : NotesResult<NotebookItemModel>.Success(NotesSupport.ToItemModel(item));
    }

    public async Task<NotesResult<NotebookItemsPageModel>> GetNotebookItemsPageAsync(
        Guid notebookId,
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        Guid? parentId = null,
        string? type = null,
        int? offset = null,
        int? limit = null)
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(notebookId, currentUserId, cancellationToken);
        if (!notebookAccessResult.Succeeded)
        {
            return NotesResult<NotebookItemsPageModel>.Failure(
                notebookAccessResult.Error!.Kind,
                notebookAccessResult.Error.Code,
                notebookAccessResult.Error.Message);
        }

        if (GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is { } archiveError)
        {
            return NotesResult<NotebookItemsPageModel>.Failure(
                archiveError.Kind,
                archiveError.Code,
                archiveError.Message);
        }

        var query = BuildNotebookItemsQuery(notebookId, search, includeArchived);
        if (parentId is not null)
        {
            query = query.Where(item => item.ParentId == parentId);
        }

        if (TryParseItemType(type, out var itemType))
        {
            query = query.Where(item => item.Type == itemType);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var normalizedOffset = Math.Max(0, offset ?? 0);
        var normalizedLimit = limit.HasValue ? Math.Max(1, limit.Value) : totalCount;
        var items = await OrderNotebookItems(query)
            .Skip(normalizedOffset)
            .Take(normalizedLimit)
            .Select(item => NotesSupport.ToItemModel(item))
            .ToListAsync(cancellationToken);

        return NotesResult<NotebookItemsPageModel>.Success(new NotebookItemsPageModel(totalCount, items));
    }

    private IQueryable<NotebookItem> BuildNotebookItemsQuery(Guid notebookId, string? search, bool includeArchived)
    {
        var normalizedSearch = NotebookInput.NormalizeSearch(search);
        var query = dbContext.NotebookItems
            .AsNoTracking()
            .Where(item => item.NotebookId == notebookId && (includeArchived || !item.IsArchived));

        if (normalizedSearch is not null)
        {
            query = ApplyItemSearch(query, normalizedSearch);
        }

        return query;
    }
}
