using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests;

// Mirrors the EF query filter: trashed pages are invisible to every read path.
// The optional notebook repository backs SearchAsync's visibility scoping; tests that never
// search can leave it null.
internal sealed class StubPageRepository(StubNotebookRepository? notebooks = null) : List<Page>, IPageRepository
{
    public HashSet<(Guid PageId, Guid UserId)> Favorites { get; } = [];

    public int ListByNotebookCallCount { get; private set; }

    // Blocks the search matches against; populated directly, mirroring StubBlockRepository data.
    public List<Block> Blocks { get; } = [];

    private IEnumerable<Page> Live => this.Where(page => page.DeletedAtUtc == null);

    public Task<Page?> FindByIdAsync(Guid pageId, CancellationToken cancellationToken)
        => Task.FromResult(Live.FirstOrDefault(page => page.Id == pageId));

    public Task LockNotebookForPageStructureAsync(Guid pageId, bool includeTrashed, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<Page?> FindBySlugAsync(Guid notebookId, string slug, CancellationToken cancellationToken)
        => Task.FromResult(Live.FirstOrDefault(page => page.NotebookId == notebookId && page.Slug == slug));

    public Task<IReadOnlyList<Page>> ListByNotebookAsync(Guid notebookId, CancellationToken cancellationToken)
    {
        ListByNotebookCallCount++;
        return Task.FromResult<IReadOnlyList<Page>>(
            Live.Where(page => page.NotebookId == notebookId).OrderBy(page => page.SortKey, StringComparer.Ordinal).ToList()
        );
    }

    public Task<IReadOnlyList<Page>> ListChildrenAsync(Guid notebookId, Guid? parentId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Page>>(
            Live.Where(page => page.NotebookId == notebookId && page.ParentId == parentId)
                .OrderBy(page => page.SortKey, StringComparer.Ordinal)
                .ToList()
        );

    public Task<Page?> FindTrashedByIdAsync(Guid pageId, CancellationToken cancellationToken)
        => Task.FromResult(this.FirstOrDefault(page => page.Id == pageId && page.DeletedAtUtc != null));

    public Task<IReadOnlyList<Page>> ListTrashedByNotebookAsync(Guid notebookId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Page>>(
            this.Where(page => page.NotebookId == notebookId && page.DeletedAtUtc != null).ToList()
        );

    // Hides List<Page>.Remove to satisfy the repository interface; the cast calls the base.
    // Mirrors the database cascade: favorites disappear with the page row.
    public new void Remove(Page page)
    {
        ((List<Page>)this).Remove(page);
        Favorites.RemoveWhere(favorite => favorite.PageId == page.Id);
    }

    public Task AddAsync(Page page, CancellationToken cancellationToken)
    {
        Add(page);
        return Task.CompletedTask;
    }

    public Task<int> CountByNotebookAsync(Guid notebookId, CancellationToken cancellationToken)
        => Task.FromResult(Live.Count(page => page.NotebookId == notebookId));

    public Task<IReadOnlyDictionary<Guid, int>> CountByNotebooksAsync(
        IReadOnlyCollection<Guid> notebookIds,
        CancellationToken cancellationToken
    ) => Task.FromResult<IReadOnlyDictionary<Guid, int>>(
        Live.Where(page => notebookIds.Contains(page.NotebookId))
            .GroupBy(page => page.NotebookId)
            .ToDictionary(group => group.Key, group => group.Count())
    );

    public Task<IReadOnlyList<PageSearchMatch>> SearchAsync(
        Guid userId,
        string query,
        DateTimeOffset? cursorUpdatedAtUtc,
        Guid? cursorId,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        if (notebooks is null)
        {
            throw new InvalidOperationException(
                "SearchAsync needs the stub notebook repository to scope visibility."
            );
        }

        // Mirrors the EF implementation: owned + notebook-share arms only, ordinal-ignore-case
        // substring matching standing in for ILIKE, first matching block ordered by SortKey.
        var notebookById = notebooks.NotebooksVisibleTo(userId).ToDictionary(notebook => notebook.Id);

        bool Matches(Block block, Page page)
            => block.PageId == page.Id && block.PlainText.Contains(query, StringComparison.OrdinalIgnoreCase);

        IEnumerable<PageSearchMatch> matches = Live
            .Where(page => notebookById.ContainsKey(page.NotebookId))
            .Where(page =>
                page.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                || Blocks.Any(block => Matches(block, page))
            )
            .Select(page => new PageSearchMatch(
                page,
                notebookById[page.NotebookId].Slug,
                notebookById[page.NotebookId].Title,
                Blocks.Where(block => Matches(block, page))
                    .OrderBy(block => block.SortKey, StringComparer.Ordinal)
                    .Select(block => (string?)block.PlainText)
                    .FirstOrDefault()
            ));

        if (cursorUpdatedAtUtc is { } updatedAtUtc && cursorId is { } id)
        {
            matches = matches.Where(match =>
                match.Page.UpdatedAtUtc < updatedAtUtc
                || (match.Page.UpdatedAtUtc == updatedAtUtc && match.Page.Id.CompareTo(id) < 0)
            );
        }

        return Task.FromResult<IReadOnlyList<PageSearchMatch>>(
            matches
                .OrderByDescending(match => match.Page.UpdatedAtUtc)
                .ThenByDescending(match => match.Page.Id)
                .Take(pageSize)
                .ToList()
        );
    }

    public Task SetFavoriteAsync(Guid pageId, Guid userId, bool isFavorite, CancellationToken cancellationToken)
    {
        if (isFavorite)
        {
            Favorites.Add((pageId, userId));
        }
        else
        {
            Favorites.Remove((pageId, userId));
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlySet<Guid>> FindFavoriteIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> pageIds,
        CancellationToken cancellationToken
    ) => Task.FromResult<IReadOnlySet<Guid>>(
        Favorites.Where(favorite => favorite.UserId == userId && pageIds.Contains(favorite.PageId))
            .Select(favorite => favorite.PageId)
            .ToHashSet()
    );

    public Task<IReadOnlyList<Page>> ListFavoritesAsync(Guid userId, Guid? notebookId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Page>>(
            Live.Where(page => (notebookId is null || page.NotebookId == notebookId) && Favorites.Contains((page.Id, userId)))
                .OrderBy(page => page.Title, StringComparer.Ordinal)
                .ToList()
        );
}
