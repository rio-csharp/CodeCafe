using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using CodeCafe.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Modules.Notes.Infrastructure.Notes;

public sealed partial class NotebookReadService(ApplicationDbContext dbContext) : INotebookReadService
{
    public Task<IReadOnlyList<NotebookSummaryModel>> GetPublicNotebooksAsync(
        string? search,
        Guid currentUserId,
        CancellationToken cancellationToken,
        int? limit = null,
        int? offset = null)
    {
        var normalizedSearch = NotebookInput.NormalizeSearch(search);
        var query = dbContext.Notebooks
            .AsNoTracking()
            .Where(notebook => notebook.Visibility == NotebookVisibility.Public && notebook.IsPublished);

        if (normalizedSearch is not null)
        {
            query = ApplyNotebookSearch(query, normalizedSearch);
        }

        return GetNotebookSummariesAsync(query, currentUserId, cancellationToken, limit, offset);
    }

    public Task<IReadOnlyList<NotebookSummaryModel>> GetMyNotebooksAsync(
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        int? limit = null,
        int? offset = null)
    {
        var normalizedSearch = NotebookInput.NormalizeSearch(search);
        var query = dbContext.Notebooks
            .AsNoTracking()
            .Where(notebook => notebook.OwnerId == currentUserId);

        if (normalizedSearch is not null)
        {
            query = ApplyNotebookSearch(query, normalizedSearch);
        }

        return GetNotebookSummariesAsync(query, currentUserId, cancellationToken, limit, offset);
    }

    private async Task<IReadOnlyList<NotebookSummaryModel>> GetNotebookSummariesAsync(
        IQueryable<Notebook> query,
        Guid currentUserId,
        CancellationToken cancellationToken,
        int? limit,
        int? offset)
    {
        if (!UsesPostgresProvider())
        {
            var notebooks = await query.ToListAsync(cancellationToken);
            var summaries = await ToSummaryModelsAsync(notebooks, currentUserId, cancellationToken);
            return ApplyPage(summaries, offset, limit);
        }

        var rows = await ApplyPage(
                BuildNotebookSummaryRows(query, currentUserId)
                    .OrderByDescending(row => row.LastItemActivityAtUtc ?? row.UpdatedAtUtc ?? row.CreatedAtUtc)
                    .ThenBy(row => row.Title)
                    .ThenBy(row => row.Id),
                offset,
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
        // Correlated scalar subqueries: Npgsql translates these reliably,
        // unlike the GroupBy+GroupJoin shape which broke with
        // "Nullable object must have a value" for notebooks without items.
        return query
            .Select(notebook => new NotebookSummaryRow
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
                ItemCount = dbContext.NotebookItems
                    .Count(item => item.NotebookId == notebook.Id && (includeArchived || !item.IsArchived)),
                FolderCount = dbContext.NotebookItems
                    .Count(item => item.NotebookId == notebook.Id && (includeArchived || !item.IsArchived) && item.Type == NotebookItemType.Folder),
                PageCount = dbContext.NotebookItems
                    .Count(item => item.NotebookId == notebook.Id && (includeArchived || !item.IsArchived) && item.Type == NotebookItemType.Page),
                FavoriteCount = dbContext.NotebookFavorites.Count(favorite => favorite.NotebookId == notebook.Id),
                IsFavoritedByMe = currentUserId != Guid.Empty
                    && dbContext.NotebookFavorites.Any(favorite => favorite.NotebookId == notebook.Id && favorite.UserId == currentUserId),
                // FirstOrDefault over an explicitly nullable projection keeps
                // notebooks with no items as null on PostgreSQL. A scalar Max
                // over an empty correlated subquery can otherwise be translated
                // into a nullable-value unwrap and return HTTP 500.
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
}

internal sealed record NotebookItemAggregate(
    Guid NotebookId,
    int ItemCount,
    int FolderCount,
    int PageCount,
    DateTimeOffset LastItemActivityAtUtc);
