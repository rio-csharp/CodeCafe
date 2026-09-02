using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using CodeCafe.Domain.Notes.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Notes.Read;

public sealed partial class NotebookReadService
{
    public async Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetPublicNotebookItemsAsync(
        string slug,
        CancellationToken cancellationToken
    )
    {
        var notebook = await GetPublicNotebookEntityBySlugAsync(slug, cancellationToken);
        if (notebook is null)
        {
            return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(
                NotesFailureKind.NotFound,
                "notebook_not_found",
                "Notebook was not found."
            );
        }

        // Public listing is metadata-only by design (decision 16); readers load a single
        // page's content through GetPublicNotebookItemAsync when they open it.
        var rows = await BuildItemRowQuery(
                OrderNotebookItems(
                    dbContext
                        .NotebookItems.AsNoTracking()
                        .Where(item => item.NotebookId == notebook.Id && !item.IsArchived)
                ),
                includeContent: false
            )
            .ToListAsync(cancellationToken);

        var items = rows.Select(row => NotesSupport.ToItemModel(row, includeContent: false)).ToList();

        return NotesResult<IReadOnlyList<NotebookItemModel>>.Success(items);
    }

    public async Task<NotesResult<NotebookItemModel>> GetPublicNotebookItemAsync(
        string slug,
        string path,
        CancellationToken cancellationToken
    )
    {
        var notebook = await GetPublicNotebookEntityBySlugAsync(slug, cancellationToken);
        if (notebook is null || !NotesSupport.TryCreatePath(path, out var pathValue))
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_item_not_found",
                "Notebook item was not found."
            );
        }

        var item = await dbContext
            .NotebookItems.AsNoTracking()
            .SingleOrDefaultAsync(
                existingItem =>
                    existingItem.NotebookId == notebook.Id
                    && existingItem.Path == pathValue
                    && !existingItem.IsArchived,
                cancellationToken
            );

        return item is null
            ? NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_item_not_found",
                "Notebook item was not found."
            )
            : NotesResult<NotebookItemModel>.Success(NotesSupport.ToItemModel(item));
    }

    public async Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetNotebookItemsAsync(
        Guid notebookId,
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeContent = false,
        int? limit = null
    )
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(
            notebookId,
            currentUserId,
            cancellationToken
        );
        if (!notebookAccessResult.Succeeded)
        {
            return Failure<IReadOnlyList<NotebookItemModel>>(notebookAccessResult.Error!);
        }

        if (
            GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is
            { } archiveError
        )
        {
            return Failure<IReadOnlyList<NotebookItemModel>>(archiveError);
        }

        var rows = await BuildItemRowQuery(
                ApplyLimit(
                    OrderNotebookItems(
                        BuildNotebookItemsQuery(notebookId, search, includeArchived)
                    ),
                    limit
                ),
                includeContent
            )
            .ToListAsync(cancellationToken);

        var items = rows.Select(row => NotesSupport.ToItemModel(row, includeContent)).ToList();

        return NotesResult<IReadOnlyList<NotebookItemModel>>.Success(items);
    }

    public async Task<NotesResult<NotebookItemModel>> GetNotebookItemByIdAsync(
        Guid notebookId,
        Guid itemId,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false
    )
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(
            notebookId,
            currentUserId,
            cancellationToken
        );
        if (!notebookAccessResult.Succeeded)
        {
            return Failure<NotebookItemModel>(notebookAccessResult.Error!);
        }

        if (
            GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is
            { } archiveError
        )
        {
            return Failure<NotebookItemModel>(archiveError);
        }

        var item = await dbContext
            .NotebookItems.AsNoTracking()
            .SingleOrDefaultAsync(
                existingItem =>
                    existingItem.NotebookId == notebookAccessResult.Value!.Id
                    && existingItem.Id == itemId
                    && (includeArchived || !existingItem.IsArchived),
                cancellationToken
            );

        return item is null
            ? NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_item_not_found",
                "Notebook item was not found."
            )
            : NotesResult<NotebookItemModel>.Success(NotesSupport.ToItemModel(item));
    }

    public async Task<NotesResult<NotebookItemModel>> GetNotebookItemByPathAsync(
        string notebookSlug,
        string path,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false
    )
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(
            notebookSlug,
            currentUserId,
            cancellationToken
        );
        if (!notebookAccessResult.Succeeded)
        {
            return Failure<NotebookItemModel>(notebookAccessResult.Error!);
        }

        if (
            GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is
            { } archiveError
        )
        {
            return Failure<NotebookItemModel>(archiveError);
        }

        if (!NotesSupport.TryCreatePath(path, out var pathValue))
        {
            return NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_item_not_found",
                "Notebook item was not found."
            );
        }

        var item = await dbContext
            .NotebookItems.AsNoTracking()
            .SingleOrDefaultAsync(
                existingItem =>
                    existingItem.NotebookId == notebookAccessResult.Value!.Id
                    && existingItem.Path == pathValue
                    && (includeArchived || !existingItem.IsArchived),
                cancellationToken
            );

        return item is null
            ? NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_item_not_found",
                "Notebook item was not found."
            )
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
        int? limit = null
    )
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(
            notebookId,
            currentUserId,
            cancellationToken
        );
        if (!notebookAccessResult.Succeeded)
        {
            return Failure<NotebookItemsPageModel>(notebookAccessResult.Error!);
        }

        if (
            GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is
            { } archiveError
        )
        {
            return Failure<NotebookItemsPageModel>(archiveError);
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
        var rows = await BuildItemRowQuery(
                OrderNotebookItems(query).Skip(normalizedOffset).Take(normalizedLimit),
                includeContent: false
            )
            .ToListAsync(cancellationToken);
        var items = rows.Select(row => NotesSupport.ToItemModel(row, includeContent: false))
            .ToList();

        return NotesResult<NotebookItemsPageModel>.Success(
            new NotebookItemsPageModel(totalCount, items)
        );
    }

    public async Task<NotesResult<NotebookFavoriteModel>> GetNotebookFavoriteStatusAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken
    )
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(
            notebookId,
            currentUserId,
            cancellationToken
        );
        if (!notebookAccessResult.Succeeded)
        {
            return Failure<NotebookFavoriteModel>(notebookAccessResult.Error!);
        }

        var favoriteModel = await BuildFavoriteModelAsync(
            notebookId,
            currentUserId,
            cancellationToken
        );

        return NotesResult<NotebookFavoriteModel>.Success(favoriteModel);
    }

    private IQueryable<NotebookItem> BuildNotebookItemsQuery(
        Guid notebookId,
        string? search,
        bool includeArchived
    )
    {
        var normalizedSearch = NotebookInput.NormalizeSearch(search);
        var query = dbContext
            .NotebookItems.AsNoTracking()
            .Where(item => item.NotebookId == notebookId && (includeArchived || !item.IsArchived));

        if (normalizedSearch is not null)
        {
            query = ApplyItemSearch(query, normalizedSearch);
        }

        return query;
    }
}
