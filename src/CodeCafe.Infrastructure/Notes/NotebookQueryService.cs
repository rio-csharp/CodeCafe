using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Notes;

public sealed class NotebookQueryService(ApplicationDbContext dbContext) : INotebookQueryService
{
    public async Task<IReadOnlyList<NotebookSummaryModel>> GetPublicNotebooksAsync(
        string? search,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = NotesSupport.NormalizeSearch(search);
        var usePostgresCaseInsensitiveSearch = UsesPostgresCaseInsensitiveSearch();
        var query = dbContext.Notebooks
            .AsNoTracking()
            .Where(notebook => notebook.Visibility == NotebookVisibility.Public && notebook.IsPublished);

        if (normalizedSearch is not null)
        {
            query = usePostgresCaseInsensitiveSearch
                ? query.Where(notebook =>
                    EF.Functions.ILike(notebook.Title, normalizedSearch)
                    || (notebook.Description != null && EF.Functions.ILike(notebook.Description, normalizedSearch)))
                : query.Where(notebook =>
                    EF.Functions.Like(notebook.Title.ToLower(), normalizedSearch.ToLower())
                    || (notebook.Description != null
                        && EF.Functions.Like(notebook.Description.ToLower(), normalizedSearch.ToLower())));
        }

        var notebooks = await query.ToListAsync(cancellationToken);
        return await ToSummaryModelsAsync(notebooks, currentUserId, cancellationToken);
    }

    public async Task<IReadOnlyList<NotebookSummaryModel>> GetMyNotebooksAsync(
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = NotesSupport.NormalizeSearch(search);
        var usePostgresCaseInsensitiveSearch = UsesPostgresCaseInsensitiveSearch();
        var query = dbContext.Notebooks
            .AsNoTracking()
            .Where(notebook => notebook.OwnerId == currentUserId);

        if (normalizedSearch is not null)
        {
            query = usePostgresCaseInsensitiveSearch
                ? query.Where(notebook =>
                    EF.Functions.ILike(notebook.Title, normalizedSearch)
                    || (notebook.Description != null && EF.Functions.ILike(notebook.Description, normalizedSearch)))
                : query.Where(notebook =>
                    EF.Functions.Like(notebook.Title.ToLower(), normalizedSearch.ToLower())
                    || (notebook.Description != null
                        && EF.Functions.Like(notebook.Description.ToLower(), normalizedSearch.ToLower())));
        }

        var notebooks = await query.ToListAsync(cancellationToken);
        return await ToSummaryModelsAsync(notebooks, currentUserId, cancellationToken);
    }

    public async Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        var notebook = await dbContext.Notebooks
            .AsNoTracking()
            .Include(existingNotebook => existingNotebook.Items.Where(item => includeArchived || !item.IsArchived))
            .SingleOrDefaultAsync(existingNotebook =>
                existingNotebook.Slug == slug
                && existingNotebook.Visibility == NotebookVisibility.Public
                && existingNotebook.IsPublished,
                cancellationToken);

        return notebook is null
            ? NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.")
            : NotesResult<NotebookDetailModel>.Success(await ToDetailModelAsync(notebook, currentUserId, cancellationToken));
    }

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

        var normalizedPath = NotesSupport.NormalizePath(path);
        var item = await dbContext.NotebookItems
            .AsNoTracking()
            .SingleOrDefaultAsync(
                existingItem => existingItem.NotebookId == notebook.Id && existingItem.Path == normalizedPath && !existingItem.IsArchived,
                cancellationToken);

        return item is null
            ? NotesResult<NotebookItemModel>.Failure(NotesFailureKind.NotFound, "notebook_item_not_found", "Notebook item was not found.")
            : NotesResult<NotebookItemModel>.Success(NotesSupport.ToItemModel(item));
    }

    public async Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        var notebook = await dbContext.Notebooks
            .AsNoTracking()
            .Include(existingNotebook => existingNotebook.Items.Where(item => includeArchived || !item.IsArchived))
            .SingleOrDefaultAsync(existingNotebook => existingNotebook.Id == notebookId, cancellationToken);

        if (notebook is null)
        {
            return NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
        }

        if (!NotesSupport.CanReadNotebook(notebook, currentUserId))
        {
            return NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.Forbidden, "notebook_forbidden", "You do not have access to this notebook.");
        }

        return NotesResult<NotebookDetailModel>.Success(await ToDetailModelAsync(notebook, currentUserId, cancellationToken));
    }

    public async Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        var notebook = await dbContext.Notebooks
            .AsNoTracking()
            .Include(existingNotebook => existingNotebook.Items.Where(item => includeArchived || !item.IsArchived))
            .SingleOrDefaultAsync(existingNotebook => existingNotebook.Slug == slug, cancellationToken);

        if (notebook is null)
        {
            return NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
        }

        if (!NotesSupport.CanReadNotebook(notebook, currentUserId))
        {
            return NotesResult<NotebookDetailModel>.Failure(NotesFailureKind.Forbidden, "notebook_forbidden", "You do not have access to this notebook.");
        }

        return NotesResult<NotebookDetailModel>.Success(await ToDetailModelAsync(notebook, currentUserId, cancellationToken));
    }

    public async Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetNotebookItemsAsync(
        Guid notebookId,
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        var notebook = await dbContext.Notebooks
            .AsNoTracking()
            .SingleOrDefaultAsync(existingNotebook => existingNotebook.Id == notebookId, cancellationToken);

        if (notebook is null)
        {
            return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
        }

        if (!NotesSupport.CanReadNotebook(notebook, currentUserId))
        {
            return NotesResult<IReadOnlyList<NotebookItemModel>>.Failure(NotesFailureKind.Forbidden, "notebook_forbidden", "You do not have access to this notebook.");
        }

        var normalizedSearch = NotesSupport.NormalizeSearch(search);
        var usePostgresCaseInsensitiveSearch = UsesPostgresCaseInsensitiveSearch();
        var query = dbContext.NotebookItems
            .AsNoTracking()
            .Where(item => item.NotebookId == notebookId && (includeArchived || !item.IsArchived));

        if (normalizedSearch is not null)
        {
            query = usePostgresCaseInsensitiveSearch
                ? query.Where(item =>
                    EF.Functions.ILike(item.Title, normalizedSearch)
                    || (item.PlainTextContent != null && EF.Functions.ILike(item.PlainTextContent, normalizedSearch)))
                : query.Where(item =>
                    EF.Functions.Like(item.Title.ToLower(), normalizedSearch.ToLower())
                    || (item.PlainTextContent != null
                        && EF.Functions.Like(item.PlainTextContent.ToLower(), normalizedSearch.ToLower())));
        }

        var items = await query
            .OrderBy(item => item.ParentId)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Title)
            .Select(item => NotesSupport.ToItemModel(item))
            .ToListAsync(cancellationToken);

        return NotesResult<IReadOnlyList<NotebookItemModel>>.Success(items);
    }

    private async Task<Notebook?> GetPublicNotebookEntityBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return await dbContext.Notebooks.AsNoTracking().SingleOrDefaultAsync(
            notebook => notebook.Slug == slug
                && notebook.Visibility == NotebookVisibility.Public
                && notebook.IsPublished,
            cancellationToken);
    }

    private bool UsesPostgresCaseInsensitiveSearch()
    {
        return string.Equals(dbContext.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal);
    }

    private async Task<IReadOnlyList<NotebookSummaryModel>> ToSummaryModelsAsync(
        IReadOnlyList<Notebook> notebooks,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var ownerIds = notebooks.Select(notebook => notebook.OwnerId).Distinct().ToList();
        var metadataByNotebookId = await GetNotebookMetadataAsync(notebooks, currentUserId, cancellationToken);
        var displayNames = await dbContext.Users
            .AsNoTracking()
            .Where(user => ownerIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);

        return notebooks
            .Select(notebook => NotesSupport.ToSummaryModel(
                notebook,
                NotesSupport.GetAuthorDisplayName(displayNames, notebook.OwnerId),
                metadataByNotebookId.GetValueOrDefault(notebook.Id) ?? NotebookMetadata.Empty,
                currentUserId))
            .OrderByDescending(response => response.LastActivityAtUtc)
            .ToList();
    }

    private async Task<NotebookDetailModel> ToDetailModelAsync(
        Notebook notebook,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var authorDisplayName = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == notebook.OwnerId)
            .Select(user => user.DisplayName)
            .SingleOrDefaultAsync(cancellationToken)
            ?? "Unknown";

        var favoriteModel = await BuildFavoriteModelAsync(notebook.Id, currentUserId, cancellationToken);
        var lastActivityAtUtc = new[]
        {
            notebook.CreatedAtUtc,
            notebook.UpdatedAtUtc ?? notebook.CreatedAtUtc
        }
        .Concat(notebook.Items.Select(item => item.UpdatedAtUtc ?? item.CreatedAtUtc))
        .Max();

        var metadata = new NotebookMetadata(
            ItemCount: notebook.Items.Count,
            FolderCount: notebook.Items.Count(item => item.Type == NotebookItemType.Folder),
            PageCount: notebook.Items.Count(item => item.Type == NotebookItemType.Page),
            FavoriteCount: favoriteModel.FavoriteCount,
            IsFavoritedByMe: favoriteModel.IsFavorited,
            LastActivityAtUtc: lastActivityAtUtc);

        return NotesSupport.ToDetailModel(notebook, authorDisplayName, metadata, currentUserId);
    }

    private async Task<IReadOnlyDictionary<Guid, NotebookMetadata>> GetNotebookMetadataAsync(
        IReadOnlyList<Notebook> notebooks,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        if (notebooks.Count == 0)
        {
            return new Dictionary<Guid, NotebookMetadata>();
        }

        var notebookIds = notebooks.Select(notebook => notebook.Id).ToList();
        var notebookItems = await dbContext.NotebookItems
            .AsNoTracking()
            .Where(item => notebookIds.Contains(item.NotebookId) && !item.IsArchived)
            .ToListAsync(cancellationToken);
        var itemAggregates = notebookItems
            .GroupBy(item => item.NotebookId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    ItemCount = group.Count(),
                    FolderCount = group.Count(item => item.Type == NotebookItemType.Folder),
                    PageCount = group.Count(item => item.Type == NotebookItemType.Page),
                    LastItemActivityAtUtc = group.Max(item => item.UpdatedAtUtc ?? item.CreatedAtUtc)
                });

        var favoriteCounts = await dbContext.NotebookFavorites
            .AsNoTracking()
            .Where(favorite => notebookIds.Contains(favorite.NotebookId))
            .GroupBy(favorite => favorite.NotebookId)
            .Select(group => new
            {
                NotebookId = group.Key,
                FavoriteCount = group.Count()
            })
            .ToDictionaryAsync(group => group.NotebookId, group => group.FavoriteCount, cancellationToken);

        var favoritedNotebookIds = currentUserId == Guid.Empty
            ? new HashSet<Guid>()
            : (await dbContext.NotebookFavorites
                .AsNoTracking()
                .Where(favorite => favorite.UserId == currentUserId && notebookIds.Contains(favorite.NotebookId))
                .Select(favorite => favorite.NotebookId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        return notebooks.ToDictionary(
            notebook => notebook.Id,
            notebook =>
            {
                var itemAggregate = itemAggregates.GetValueOrDefault(notebook.Id);
                var lastActivityAtUtc = new[]
                {
                    notebook.CreatedAtUtc,
                    notebook.UpdatedAtUtc ?? notebook.CreatedAtUtc,
                    itemAggregate?.LastItemActivityAtUtc ?? notebook.CreatedAtUtc
                }.Max();

                return new NotebookMetadata(
                    ItemCount: itemAggregate?.ItemCount ?? 0,
                    FolderCount: itemAggregate?.FolderCount ?? 0,
                    PageCount: itemAggregate?.PageCount ?? 0,
                    FavoriteCount: favoriteCounts.GetValueOrDefault(notebook.Id),
                    IsFavoritedByMe: favoritedNotebookIds.Contains(notebook.Id),
                    LastActivityAtUtc: lastActivityAtUtc);
            });
    }

    private async Task<NotebookFavoriteModel> BuildFavoriteModelAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var favoriteCount = await dbContext.NotebookFavorites
            .AsNoTracking()
            .CountAsync(favorite => favorite.NotebookId == notebookId, cancellationToken);
        var isFavorited = currentUserId != Guid.Empty && await dbContext.NotebookFavorites
            .AsNoTracking()
            .AnyAsync(favorite => favorite.NotebookId == notebookId && favorite.UserId == currentUserId, cancellationToken);

        return new NotebookFavoriteModel(notebookId, isFavorited, favoriteCount);
    }
}
