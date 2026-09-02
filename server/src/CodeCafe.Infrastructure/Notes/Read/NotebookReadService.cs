using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using CodeCafe.Domain.Notes.Enums;
using CodeCafe.Domain.Notes.ValueObjects;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Notes.Read;

public sealed partial class NotebookReadService(
    ApplicationDbContext dbContext,
    INotebookAccessCodeHasher accessCodeHasher
)
    : INotebookReadService
{
    public Task<IReadOnlyList<NotebookSummaryModel>> GetPublicNotebooksAsync(
        string? search,
        Guid currentUserId,
        CancellationToken cancellationToken,
        int? limit = null,
        int? offset = null
    )
    {
        var normalizedSearch = NotebookInput.NormalizeSearch(search);
        var query = dbContext
            .Notebooks.AsNoTracking()
            .Where(notebook => notebook.Visibility == NotebookVisibility.Public);

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
        int? offset = null
    )
    {
        var normalizedSearch = NotebookInput.NormalizeSearch(search);
        var query = dbContext
            .Notebooks.AsNoTracking()
            .Where(notebook => notebook.OwnerId == currentUserId);

        if (normalizedSearch is not null)
        {
            query = ApplyNotebookSearch(query, normalizedSearch);
        }

        return GetNotebookSummariesAsync(query, currentUserId, cancellationToken, limit, offset);
    }

    public Task<IReadOnlyList<NotebookSummaryModel>> GetFavoriteNotebooksAsync(
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        int? limit = null,
        int? offset = null
    )
    {
        var normalizedSearch = NotebookInput.NormalizeSearch(search);
        var favoritedIds = dbContext
            .NotebookFavorites.AsNoTracking()
            .Where(favorite => favorite.UserId == currentUserId)
            .Select(favorite => favorite.NotebookId);

        // A favorite can outlive its notebook's accessibility (e.g. it later turned private),
        // so the list re-filters through the same read rules as everything else.
        var query = dbContext
            .Notebooks.AsNoTracking()
            .Where(notebook => favoritedIds.Contains(notebook.Id))
            .Where(NotebookAccessPredicates.ReadableByUser(
                dbContext, currentUserId, includeUnlisted: true));

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
        int? offset
    )
    {
        var rows = await ApplyPage(
                BuildNotebookSummaryRows(query, currentUserId)
                    .OrderByDescending(row =>
                        row.LastItemActivityAtUtc ?? row.UpdatedAtUtc ?? row.CreatedAtUtc
                    )
                    .ThenBy(row => row.Title)
                    .ThenBy(row => row.Id),
                offset,
                limit
            )
            .ToListAsync(cancellationToken);

        return rows.Select(ToSummaryModel).ToList();
    }

    private IQueryable<NotebookSummaryRow> BuildNotebookSummaryRows(
        IQueryable<Notebook> query,
        Guid currentUserId,
        bool includeArchived = false
    )
    {
        return query.Select(notebook => new NotebookSummaryRow
        {
            Id = notebook.Id,
            OwnerId = notebook.OwnerId,
            Title = notebook.Title,
            Slug = notebook.Slug,
            Description = notebook.Description,
            Visibility = notebook.Visibility,
            AuthorDisplayName = dbContext
                .Users.Where(user => user.Id == notebook.OwnerId)
                .Select(user => user.DisplayName)
                .FirstOrDefault(),
            CanEdit = notebook.OwnerId == currentUserId,
            ItemCount = dbContext.NotebookItems.Count(item =>
                item.NotebookId == notebook.Id && (includeArchived || !item.IsArchived)
            ),
            FolderCount = dbContext.NotebookItems.Count(item =>
                item.NotebookId == notebook.Id
                && (includeArchived || !item.IsArchived)
                && item.Type == NotebookItemType.Folder
            ),
            PageCount = dbContext.NotebookItems.Count(item =>
                item.NotebookId == notebook.Id
                && (includeArchived || !item.IsArchived)
                && item.Type == NotebookItemType.Page
            ),
            FavoriteCount = dbContext.NotebookFavorites.Count(favorite =>
                favorite.NotebookId == notebook.Id
            ),
            IsFavoritedByMe =
                currentUserId != Guid.Empty
                && dbContext.NotebookFavorites.Any(favorite =>
                    favorite.NotebookId == notebook.Id && favorite.UserId == currentUserId
                ),
            LastItemActivityAtUtc = dbContext
                .NotebookItems.Where(item =>
                    item.NotebookId == notebook.Id && (includeArchived || !item.IsArchived)
                )
                .OrderByDescending(item => item.UpdatedAtUtc ?? item.CreatedAtUtc)
                .Select(item => (DateTimeOffset?)(item.UpdatedAtUtc ?? item.CreatedAtUtc))
                .FirstOrDefault(),
            CreatedAtUtc = notebook.CreatedAtUtc,
            UpdatedAtUtc = notebook.UpdatedAtUtc,
            PublishedAtUtc = notebook.PublishedAtUtc,
        });
    }

    private static NotebookSummaryModel ToSummaryModel(NotebookSummaryRow row)
    {
        return new NotebookSummaryModel(
            row.Id,
            row.OwnerId,
            row.Title,
            row.Slug.Value,
            row.Description,
            row.Visibility.ToString().ToLowerInvariant(),
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
            row.PublishedAtUtc
        );
    }

    private async Task<IReadOnlyDictionary<Guid, NotebookMetadata>> GetNotebookMetadataAsync(
        IReadOnlyList<Notebook> notebooks,
        Guid currentUserId,
        CancellationToken cancellationToken,
        bool includeArchived = false
    )
    {
        if (notebooks.Count == 0)
        {
            return new Dictionary<Guid, NotebookMetadata>();
        }

        var notebookIds = notebooks.Select(notebook => notebook.Id).ToList();
        var itemAggregates = await GetNotebookItemAggregatesFromDatabaseAsync(
            notebookIds,
            cancellationToken,
            includeArchived
        );

        var favoriteCounts = await dbContext
            .NotebookFavorites.AsNoTracking()
            .Where(favorite => notebookIds.Contains(favorite.NotebookId))
            .GroupBy(favorite => favorite.NotebookId)
            .Select(group => new { NotebookId = group.Key, FavoriteCount = group.Count() })
            .ToDictionaryAsync(
                group => group.NotebookId,
                group => group.FavoriteCount,
                cancellationToken
            );

        var favoritedNotebookIds =
            currentUserId == Guid.Empty
                ? new HashSet<Guid>()
                : (
                    await dbContext
                        .NotebookFavorites.AsNoTracking()
                        .Where(favorite =>
                            favorite.UserId == currentUserId
                            && notebookIds.Contains(favorite.NotebookId)
                        )
                        .Select(favorite => favorite.NotebookId)
                        .ToListAsync(cancellationToken)
                ).ToHashSet();

        return notebooks.ToDictionary(
            notebook => notebook.Id,
            notebook =>
            {
                var itemAggregate = itemAggregates.GetValueOrDefault(notebook.Id);
                var lastActivityAtUtc = new[]
                {
                    notebook.CreatedAtUtc,
                    notebook.UpdatedAtUtc ?? notebook.CreatedAtUtc,
                    itemAggregate?.LastItemActivityAtUtc ?? notebook.CreatedAtUtc,
                }.Max();

                return new NotebookMetadata(
                    ItemCount: itemAggregate?.ItemCount ?? 0,
                    FolderCount: itemAggregate?.FolderCount ?? 0,
                    PageCount: itemAggregate?.PageCount ?? 0,
                    FavoriteCount: favoriteCounts.GetValueOrDefault(notebook.Id),
                    IsFavoritedByMe: favoritedNotebookIds.Contains(notebook.Id),
                    LastActivityAtUtc: lastActivityAtUtc
                );
            }
        );
    }

    private async Task<
        IReadOnlyDictionary<Guid, NotebookItemAggregate>
    > GetNotebookItemAggregatesFromDatabaseAsync(
        IReadOnlyList<Guid> notebookIds,
        CancellationToken cancellationToken,
        bool includeArchived = false
    )
    {
        return await dbContext
            .NotebookItems.AsNoTracking()
            .Where(item =>
                notebookIds.Contains(item.NotebookId) && (includeArchived || !item.IsArchived)
            )
            .GroupBy(item => item.NotebookId)
            .Select(group => new NotebookItemAggregate(
                group.Key,
                group.Count(),
                group.Count(item => item.Type == NotebookItemType.Folder),
                group.Count(item => item.Type == NotebookItemType.Page),
                group.Max(item => item.UpdatedAtUtc ?? item.CreatedAtUtc)
            ))
            .ToDictionaryAsync(group => group.NotebookId, cancellationToken);
    }


    private sealed class NotebookSummaryRow
    {
        public Guid Id { get; init; }

        public Guid OwnerId { get; init; }

        public string Title { get; init; } = string.Empty;

        public NotebookSlug Slug { get; init; } = null!;

        public string? Description { get; init; }

        public NotebookVisibility Visibility { get; init; }

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
    DateTimeOffset LastItemActivityAtUtc
);
