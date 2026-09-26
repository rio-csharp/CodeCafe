using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests;

// Mirrors the EF query filter: trashed pages are invisible to every read path.
internal sealed class StubPageRepository : List<Page>, IPageRepository
{
    public HashSet<(Guid PageId, Guid UserId)> Favorites { get; } = [];

    private IEnumerable<Page> Live => this.Where(page => page.DeletedAtUtc == null);

    public Task<Page?> FindByIdAsync(Guid pageId, CancellationToken cancellationToken)
        => Task.FromResult(Live.FirstOrDefault(page => page.Id == pageId));

    public Task<Page?> FindBySlugAsync(Guid notebookId, string slug, CancellationToken cancellationToken)
        => Task.FromResult(Live.FirstOrDefault(page => page.NotebookId == notebookId && page.Slug == slug));

    public Task<IReadOnlyList<Page>> ListByNotebookAsync(Guid notebookId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Page>>(
            Live.Where(page => page.NotebookId == notebookId).OrderBy(page => page.SortKey, StringComparer.Ordinal).ToList()
        );

    public Task<IReadOnlyList<Page>> ListSiblingsAsync(Guid notebookId, Guid? parentId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Page>>(
            Live.Where(page => page.NotebookId == notebookId && page.ParentId == parentId)
                .OrderBy(page => page.SortKey, StringComparer.Ordinal)
                .ToList()
        );

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
