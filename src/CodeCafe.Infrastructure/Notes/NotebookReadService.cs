using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Notes;

public sealed class NotebookReadService(ApplicationDbContext dbContext) : INotebookReadService
{
    public Task<IReadOnlyList<NotebookSummaryModel>> GetPublicNotebooksAsync(
        string? search,
        Guid currentUserId,
        CancellationToken cancellationToken,
        int? limit = null)
    {
        var normalizedSearch = NotebookInput.NormalizeSearch(search);
        var query = dbContext.Notebooks
            .AsNoTracking()
            .Where(notebook => notebook.Visibility == NotebookVisibility.Public && notebook.IsPublished);

        if (normalizedSearch is not null)
        {
            query = ApplyNotebookSearch(query, normalizedSearch);
        }

        return GetNotebookSummariesAsync(query, currentUserId, cancellationToken, limit);
    }

    public Task<IReadOnlyList<NotebookSummaryModel>> GetMyNotebooksAsync(
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        int? limit = null)
    {
        var normalizedSearch = NotebookInput.NormalizeSearch(search);
        var query = dbContext.Notebooks
            .AsNoTracking()
            .Where(notebook => notebook.OwnerId == currentUserId);

        if (normalizedSearch is not null)
        {
            query = ApplyNotebookSearch(query, normalizedSearch);
        }

        return GetNotebookSummariesAsync(query, currentUserId, cancellationToken, limit);
    }

    public async Task<IReadOnlyList<NotebookItemSearchModel>> SearchVisibleNotebookItemsAsync(
        Guid currentUserId,
        string search,
        CancellationToken cancellationToken,
        int? limit = null)
    {
        var normalizedSearch = NotebookInput.NormalizeSearch(search);
        if (normalizedSearch is null)
        {
            return [];
        }

        var query = dbContext.NotebookItems
            .AsNoTracking()
            .Where(item => !item.IsArchived)
            .Where(item =>
                item.Notebook.OwnerId == currentUserId
                || (item.Notebook.Visibility == NotebookVisibility.Public && item.Notebook.IsPublished));

        query = ApplyItemSearch(query, normalizedSearch);

        return await ApplyLimit(
                query.OrderBy(item => item.Notebook.Title)
                    .ThenBy(item => item.NotebookId)
                    .ThenBy(item => item.Path),
                limit)
            .Select(item => new NotebookItemSearchModel(
                item.NotebookId,
                item.Notebook.Slug,
                item.Notebook.Title,
                item.Notebook.OwnerId == currentUserId,
                item.Id,
                item.Path,
                item.Title,
                item.Type.ToString().ToLowerInvariant(),
                item.PlainTextContent,
                item.CreatedAtUtc,
                item.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true)
    {
        var notebookAccessResult = await GetPublicReadableNotebookAccessAsync(slug, currentUserId, cancellationToken);
        if (!notebookAccessResult.Succeeded)
        {
            return NotesResult<NotebookDetailModel>.Failure(
                notebookAccessResult.Error!.Kind,
                notebookAccessResult.Error.Code,
                notebookAccessResult.Error.Message);
        }

        if (GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is { } archiveError)
        {
            return NotesResult<NotebookDetailModel>.Failure(
                archiveError.Kind,
                archiveError.Code,
                archiveError.Message);
        }

        var query = dbContext.Notebooks
            .AsNoTracking()
            .Where(existingNotebook => existingNotebook.Id == notebookAccessResult.Value!.Id);

        if (includeItems)
        {
            query = query.Include(existingNotebook => existingNotebook.Items.Where(item => includeArchived || !item.IsArchived));
        }

        var notebook = await query.SingleAsync(cancellationToken);

        return NotesResult<NotebookDetailModel>.Success(await ToDetailModelAsync(notebook, currentUserId, cancellationToken, includeArchived, includeItems));
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

    public async Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true)
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(notebookId, currentUserId, cancellationToken);
        if (!notebookAccessResult.Succeeded)
        {
            return NotesResult<NotebookDetailModel>.Failure(
                notebookAccessResult.Error!.Kind,
                notebookAccessResult.Error.Code,
                notebookAccessResult.Error.Message);
        }

        if (GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is { } archiveError)
        {
            return NotesResult<NotebookDetailModel>.Failure(
                archiveError.Kind,
                archiveError.Code,
                archiveError.Message);
        }

        var query = dbContext.Notebooks
            .AsNoTracking()
            .Where(existingNotebook => existingNotebook.Id == notebookId);

        if (includeItems)
        {
            query = query.Include(existingNotebook => existingNotebook.Items.Where(item => includeArchived || !item.IsArchived));
        }

        var notebook = await query.SingleAsync(cancellationToken);

        return NotesResult<NotebookDetailModel>.Success(await ToDetailModelAsync(notebook, currentUserId, cancellationToken, includeArchived, includeItems));
    }

    public async Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        bool includeItems = true)
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(slug, currentUserId, cancellationToken);
        if (!notebookAccessResult.Succeeded)
        {
            return NotesResult<NotebookDetailModel>.Failure(
                notebookAccessResult.Error!.Kind,
                notebookAccessResult.Error.Code,
                notebookAccessResult.Error.Message);
        }

        if (GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is { } archiveError)
        {
            return NotesResult<NotebookDetailModel>.Failure(
                archiveError.Kind,
                archiveError.Code,
                archiveError.Message);
        }

        var query = dbContext.Notebooks
            .AsNoTracking()
            .Where(existingNotebook => existingNotebook.Id == notebookAccessResult.Value!.Id);

        if (includeItems)
        {
            query = query.Include(existingNotebook => existingNotebook.Items.Where(item => includeArchived || !item.IsArchived));
        }

        var notebook = await query.SingleAsync(cancellationToken);

        return NotesResult<NotebookDetailModel>.Success(await ToDetailModelAsync(notebook, currentUserId, cancellationToken, includeArchived, includeItems));
    }

    public async Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetNotebookItemsAsync(
        Guid notebookId,
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        bool includeArchived = false,
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

        var items = await ApplyLimit(
                OrderNotebookItems(BuildNotebookItemsQuery(notebookId, search, includeArchived)),
                limit)
            .Select(item => NotesSupport.ToItemModel(item))
            .ToListAsync(cancellationToken);

        return NotesResult<IReadOnlyList<NotebookItemModel>>.Success(items);
    }

    public async Task<NotesResult<NotebookSummaryModel>> GetNotebookSummaryBySlugAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        var notebookAccessResult = await GetReadableNotebookAccessAsync(slug, currentUserId, cancellationToken);
        if (!notebookAccessResult.Succeeded)
        {
            return NotesResult<NotebookSummaryModel>.Failure(
                notebookAccessResult.Error!.Kind,
                notebookAccessResult.Error.Code,
                notebookAccessResult.Error.Message);
        }

        if (GetArchivedReadError(notebookAccessResult.Value!, currentUserId, includeArchived) is { } archiveError)
        {
            return NotesResult<NotebookSummaryModel>.Failure(
                archiveError.Kind,
                archiveError.Code,
                archiveError.Message);
        }

        if (!UsesPostgresProvider())
        {
            var notebook = await dbContext.Notebooks
                .AsNoTracking()
                .SingleAsync(existingNotebook => existingNotebook.Id == notebookAccessResult.Value!.Id, cancellationToken);
            var summaries = await ToSummaryModelsAsync([notebook], currentUserId, cancellationToken, includeArchived);
            return NotesResult<NotebookSummaryModel>.Success(summaries.Single());
        }

        var summary = await BuildNotebookSummaryRows(
                dbContext.Notebooks
                    .AsNoTracking()
                    .Where(notebook => notebook.Id == notebookAccessResult.Value!.Id),
                currentUserId,
                includeArchived)
            .SingleAsync(cancellationToken);

        return NotesResult<NotebookSummaryModel>.Success(ToSummaryModel(summary));
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

    private async Task<IReadOnlyList<NotebookSummaryModel>> GetNotebookSummariesAsync(
        IQueryable<Notebook> query,
        Guid currentUserId,
        CancellationToken cancellationToken,
        int? limit)
    {
        if (!UsesPostgresProvider())
        {
            var notebooks = await query.ToListAsync(cancellationToken);
            var summaries = await ToSummaryModelsAsync(notebooks, currentUserId, cancellationToken);
            return ApplyLimit(summaries, limit);
        }

        var rows = await ApplyLimit(
                BuildNotebookSummaryRows(query, currentUserId)
                    .OrderByDescending(row => row.LastItemActivityAtUtc ?? row.UpdatedAtUtc ?? row.CreatedAtUtc)
                    .ThenBy(row => row.Title),
                limit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(ToSummaryModel)
            .ToList();
    }

    private IQueryable<NotebookSummaryRow> BuildNotebookSummaryRows(
        IQueryable<Notebook> query,
        Guid currentUserId,
        bool includeArchived = false)
    {
        return query.Select(notebook => new NotebookSummaryRow
        {
            Id = notebook.Id,
            OwnerId = notebook.OwnerId,
            Title = notebook.Title,
            Slug = notebook.Slug,
            Description = notebook.Description,
            Visibility = notebook.Visibility,
            IsPublished = notebook.IsPublished,
            AuthorDisplayName = dbContext.Users
                .Where(user => user.Id == notebook.OwnerId)
                .Select(user => user.DisplayName)
                .FirstOrDefault(),
            CanEdit = notebook.OwnerId == currentUserId,
            ItemCount = dbContext.NotebookItems.Count(item => item.NotebookId == notebook.Id && (includeArchived || !item.IsArchived)),
            FolderCount = dbContext.NotebookItems.Count(item => item.NotebookId == notebook.Id && (includeArchived || !item.IsArchived) && item.Type == NotebookItemType.Folder),
            PageCount = dbContext.NotebookItems.Count(item => item.NotebookId == notebook.Id && (includeArchived || !item.IsArchived) && item.Type == NotebookItemType.Page),
            FavoriteCount = dbContext.NotebookFavorites.Count(favorite => favorite.NotebookId == notebook.Id),
            IsFavoritedByMe = currentUserId != Guid.Empty
                && dbContext.NotebookFavorites.Any(favorite => favorite.NotebookId == notebook.Id && favorite.UserId == currentUserId),
            LastItemActivityAtUtc = dbContext.NotebookItems
                .Where(item => item.NotebookId == notebook.Id && (includeArchived || !item.IsArchived))
                .OrderByDescending(item => item.UpdatedAtUtc ?? item.CreatedAtUtc)
                .Select(item => (DateTimeOffset?)(item.UpdatedAtUtc ?? item.CreatedAtUtc))
                .FirstOrDefault(),
            CreatedAtUtc = notebook.CreatedAtUtc,
            UpdatedAtUtc = notebook.UpdatedAtUtc,
            PublishedAtUtc = notebook.PublishedAtUtc
        });
    }

    private static NotebookSummaryModel ToSummaryModel(NotebookSummaryRow row)
    {
        return new NotebookSummaryModel(
            row.Id,
            row.OwnerId,
            row.Title,
            row.Slug,
            row.Description,
            row.Visibility.ToString().ToLowerInvariant(),
            row.IsPublished,
            row.AuthorDisplayName ?? "Unknown",
            row.CanEdit,
            row.ItemCount,
            row.FolderCount,
            row.PageCount,
            row.FavoriteCount,
            row.IsFavoritedByMe,
            row.LastItemActivityAtUtc ?? row.UpdatedAtUtc ?? row.CreatedAtUtc,
            row.CreatedAtUtc,
            row.UpdatedAtUtc,
            row.PublishedAtUtc);
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

    private async Task<Notebook?> GetPublicNotebookEntityBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return await dbContext.Notebooks.AsNoTracking().SingleOrDefaultAsync(
            notebook => notebook.Slug == slug
                && notebook.Visibility == NotebookVisibility.Public
                && notebook.IsPublished,
            cancellationToken);
    }

    private async Task<NotesResult<NotebookAccessRow>> GetPublicReadableNotebookAccessAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var notebook = await dbContext.Notebooks
            .AsNoTracking()
            .Where(existingNotebook =>
                existingNotebook.Slug == slug
                && existingNotebook.Visibility == NotebookVisibility.Public
                && existingNotebook.IsPublished)
            .Select(existingNotebook => new NotebookAccessRow(existingNotebook.Id, existingNotebook.OwnerId, existingNotebook.Visibility, existingNotebook.IsPublished))
            .SingleOrDefaultAsync(cancellationToken);

        return ToReadableNotebookAccessResult(notebook, currentUserId);
    }

    private async Task<NotesResult<NotebookAccessRow>> GetReadableNotebookAccessAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var notebook = await dbContext.Notebooks
            .AsNoTracking()
            .Where(existingNotebook => existingNotebook.Id == notebookId)
            .Select(existingNotebook => new NotebookAccessRow(existingNotebook.Id, existingNotebook.OwnerId, existingNotebook.Visibility, existingNotebook.IsPublished))
            .SingleOrDefaultAsync(cancellationToken);

        return ToReadableNotebookAccessResult(notebook, currentUserId);
    }

    private async Task<NotesResult<NotebookAccessRow>> GetReadableNotebookAccessAsync(
        string slug,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var notebook = await dbContext.Notebooks
            .AsNoTracking()
            .Where(existingNotebook => existingNotebook.Slug == slug)
            .Select(existingNotebook => new NotebookAccessRow(existingNotebook.Id, existingNotebook.OwnerId, existingNotebook.Visibility, existingNotebook.IsPublished))
            .SingleOrDefaultAsync(cancellationToken);

        return ToReadableNotebookAccessResult(notebook, currentUserId);
    }

    private static NotesResult<NotebookAccessRow> ToReadableNotebookAccessResult(NotebookAccessRow? notebook, Guid currentUserId)
    {
        if (notebook is null)
        {
            return NotesResult<NotebookAccessRow>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
        }

        return NotebookAccessPolicy.CanReadNotebook(notebook.OwnerId, notebook.Visibility, notebook.IsPublished, currentUserId)
            ? NotesResult<NotebookAccessRow>.Success(notebook)
            : NotesResult<NotebookAccessRow>.Failure(NotesFailureKind.Forbidden, "notebook_forbidden", "You do not have access to this notebook.");
    }

    private static NotesError? GetArchivedReadError(
        NotebookAccessRow notebook,
        Guid currentUserId,
        bool includeArchived)
    {
        return includeArchived && notebook.OwnerId != currentUserId
            ? new NotesError(
                NotesFailureKind.Forbidden,
                "notebook_forbidden",
                "Only the notebook owner can view archived items.")
            : null;
    }

    private bool UsesPostgresProvider()
    {
        return string.Equals(dbContext.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal);
    }

    private IQueryable<Notebook> ApplyNotebookSearch(IQueryable<Notebook> query, string normalizedSearch)
    {
        return UsesPostgresProvider()
            ? query.Where(notebook =>
                EF.Functions.ILike(notebook.Title, normalizedSearch)
                || (notebook.Description != null && EF.Functions.ILike(notebook.Description, normalizedSearch)))
            : query.Where(notebook =>
                EF.Functions.Like(notebook.Title.ToLower(), normalizedSearch.ToLower())
                || (notebook.Description != null
                    && EF.Functions.Like(notebook.Description.ToLower(), normalizedSearch.ToLower())));
    }

    private IQueryable<NotebookItem> ApplyItemSearch(IQueryable<NotebookItem> query, string normalizedSearch)
    {
        return UsesPostgresProvider()
            ? query.Where(item =>
                EF.Functions.ILike(item.Title, normalizedSearch)
                || (item.PlainTextContent != null && EF.Functions.ILike(item.PlainTextContent, normalizedSearch)))
            : query.Where(item =>
                EF.Functions.Like(item.Title.ToLower(), normalizedSearch.ToLower())
                || (item.PlainTextContent != null
                    && EF.Functions.Like(item.PlainTextContent.ToLower(), normalizedSearch.ToLower())));
    }

    private static IOrderedQueryable<NotebookItem> OrderNotebookItems(IQueryable<NotebookItem> query)
    {
        return query
            .OrderBy(item => item.ParentId)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Title);
    }

    private static bool TryParseItemType(string? type, out NotebookItemType itemType)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            itemType = default;
            return false;
        }

        return Enum.TryParse(type, ignoreCase: true, out itemType) && Enum.IsDefined(itemType);
    }

    private static IQueryable<T> ApplyLimit<T>(IQueryable<T> query, int? limit)
    {
        return limit.HasValue
            ? query.Take(Math.Max(1, limit.Value))
            : query;
    }

    private static IReadOnlyList<T> ApplyLimit<T>(IReadOnlyList<T> values, int? limit)
    {
        return limit.HasValue
            ? values.Take(Math.Max(1, limit.Value)).ToList()
            : values;
    }

    private async Task<IReadOnlyList<NotebookSummaryModel>> ToSummaryModelsAsync(
        IReadOnlyList<Notebook> notebooks,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        var ownerIds = notebooks.Select(notebook => notebook.OwnerId).Distinct().ToList();
        var metadataByNotebookId = await GetNotebookMetadataAsync(notebooks, currentUserId, cancellationToken, includeArchived);
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
            .ThenBy(response => response.Title)
            .ToList();
    }

    private async Task<IReadOnlyDictionary<Guid, NotebookMetadata>> GetNotebookMetadataAsync(
        IReadOnlyList<Notebook> notebooks,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        if (notebooks.Count == 0)
        {
            return new Dictionary<Guid, NotebookMetadata>();
        }

        var notebookIds = notebooks.Select(notebook => notebook.Id).ToList();
        var itemAggregates = UsesPostgresProvider()
            ? await GetNotebookItemAggregatesFromDatabaseAsync(notebookIds, cancellationToken, includeArchived)
            : await GetNotebookItemAggregatesInMemoryAsync(notebookIds, cancellationToken, includeArchived);

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

    private async Task<NotebookDetailModel> ToDetailModelAsync(
        Notebook notebook,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived,
        bool includeItems)
    {
        var authorDisplayName = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == notebook.OwnerId)
            .Select(user => user.DisplayName)
            .SingleOrDefaultAsync(cancellationToken)
            ?? "Unknown";

        var metadata = includeItems
            ? await BuildLoadedDetailMetadataAsync(notebook, currentUserId, cancellationToken)
            : (await GetNotebookMetadataAsync([notebook], currentUserId, cancellationToken, includeArchived)).GetValueOrDefault(notebook.Id) ?? NotebookMetadata.Empty;

        return NotesSupport.ToDetailModel(notebook, authorDisplayName, metadata, currentUserId);
    }

    private async Task<NotebookMetadata> BuildLoadedDetailMetadataAsync(
        Notebook notebook,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var favoriteModel = await BuildFavoriteModelAsync(notebook.Id, currentUserId, cancellationToken);
        var lastActivityAtUtc = new[]
        {
            notebook.CreatedAtUtc,
            notebook.UpdatedAtUtc ?? notebook.CreatedAtUtc
        }
        .Concat(notebook.Items.Select(item => item.UpdatedAtUtc ?? item.CreatedAtUtc))
        .Max();

        return new NotebookMetadata(
            ItemCount: notebook.Items.Count,
            FolderCount: notebook.Items.Count(item => item.Type == NotebookItemType.Folder),
            PageCount: notebook.Items.Count(item => item.Type == NotebookItemType.Page),
            FavoriteCount: favoriteModel.FavoriteCount,
            IsFavoritedByMe: favoriteModel.IsFavorited,
            LastActivityAtUtc: lastActivityAtUtc);
    }

    private async Task<NotebookFavoriteModel> BuildFavoriteModelAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var favoriteCount = await dbContext.NotebookFavorites
            .AsNoTracking()
            .CountAsync(favorite => favorite.NotebookId == notebookId, cancellationToken);

        var isFavorited = currentUserId != Guid.Empty
            && await dbContext.NotebookFavorites
                .AsNoTracking()
                .AnyAsync(
                    favorite => favorite.NotebookId == notebookId && favorite.UserId == currentUserId,
                    cancellationToken);

        return new NotebookFavoriteModel(notebookId, isFavorited, favoriteCount);
    }

    private async Task<IReadOnlyDictionary<Guid, NotebookItemAggregate>> GetNotebookItemAggregatesFromDatabaseAsync(
        IReadOnlyList<Guid> notebookIds,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        return await dbContext.NotebookItems
            .AsNoTracking()
            .Where(item => notebookIds.Contains(item.NotebookId) && (includeArchived || !item.IsArchived))
            .GroupBy(item => item.NotebookId)
            .Select(group => new NotebookItemAggregate(
                group.Key,
                group.Count(),
                group.Count(item => item.Type == NotebookItemType.Folder),
                group.Count(item => item.Type == NotebookItemType.Page),
                group.Max(item => item.UpdatedAtUtc ?? item.CreatedAtUtc)))
            .ToDictionaryAsync(group => group.NotebookId, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, NotebookItemAggregate>> GetNotebookItemAggregatesInMemoryAsync(
        IReadOnlyList<Guid> notebookIds,
        CancellationToken cancellationToken,
        bool includeArchived = false)
    {
        var notebookItems = await dbContext.NotebookItems
            .AsNoTracking()
            .Where(item => notebookIds.Contains(item.NotebookId) && (includeArchived || !item.IsArchived))
            .ToListAsync(cancellationToken);

        return notebookItems
            .GroupBy(item => item.NotebookId)
            .ToDictionary(
                group => group.Key,
                group => new NotebookItemAggregate(
                    group.Key,
                    group.Count(),
                    group.Count(item => item.Type == NotebookItemType.Folder),
                    group.Count(item => item.Type == NotebookItemType.Page),
                    group.Max(item => item.UpdatedAtUtc ?? item.CreatedAtUtc)));
    }

    private sealed class NotebookSummaryRow
    {
        public Guid Id { get; init; }

        public Guid OwnerId { get; init; }

        public string Title { get; init; } = string.Empty;

        public string Slug { get; init; } = string.Empty;

        public string? Description { get; init; }

        public NotebookVisibility Visibility { get; init; }

        public bool IsPublished { get; init; }

        public string? AuthorDisplayName { get; init; }

        public bool CanEdit { get; init; }

        public int ItemCount { get; init; }

        public int FolderCount { get; init; }

        public int PageCount { get; init; }

        public int FavoriteCount { get; init; }

        public bool IsFavoritedByMe { get; init; }

        public DateTimeOffset? LastItemActivityAtUtc { get; init; }

        public DateTimeOffset CreatedAtUtc { get; init; }

        public DateTimeOffset? UpdatedAtUtc { get; init; }

        public DateTimeOffset? PublishedAtUtc { get; init; }
    }

    private sealed record NotebookAccessRow(
        Guid Id,
        Guid OwnerId,
        NotebookVisibility Visibility,
        bool IsPublished);
}

internal sealed record NotebookItemAggregate(
    Guid NotebookId,
    int ItemCount,
    int FolderCount,
    int PageCount,
    DateTimeOffset LastItemActivityAtUtc);
