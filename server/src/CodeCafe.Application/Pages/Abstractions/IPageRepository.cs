using CodeCafe.Application.Pages.Shared;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Pages.Abstractions;

public interface IPageRepository
{
    Task<Page?> FindByIdAsync(Guid pageId, CancellationToken cancellationToken);

    Task<Page?> FindBySlugAsync(Guid notebookId, string slug, CancellationToken cancellationToken);

    // Non-trashed pages of the notebook, ordered by SortKey; tree assembly happens in memory.
    Task<IReadOnlyList<Page>> ListByNotebookAsync(Guid notebookId, CancellationToken cancellationToken);

    // Trashed pages are invisible to normal queries; these two see past the filter.
    Task<Page?> FindTrashedByIdAsync(Guid pageId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Page>> ListTrashedByNotebookAsync(Guid notebookId, CancellationToken cancellationToken);

    void Remove(Page page);

    // Non-trashed children of one parent (null = notebook root), ordered by SortKey.
    Task<IReadOnlyList<Page>> ListChildrenAsync(Guid notebookId, Guid? parentId, CancellationToken cancellationToken);

    Task AddAsync(Page page, CancellationToken cancellationToken);

    Task<int> CountByNotebookAsync(Guid notebookId, CancellationToken cancellationToken);

    // One GROUP BY round-trip for trash listings, instead of a count query per notebook.
    Task<IReadOnlyDictionary<Guid, int>> CountByNotebooksAsync(
        IReadOnlyCollection<Guid> notebookIds,
        CancellationToken cancellationToken
    );

    // Favorites are per-user; setting one that already exists (or clearing an absent one) is a no-op.
    Task SetFavoriteAsync(Guid pageId, Guid userId, bool isFavorite, CancellationToken cancellationToken);

    Task<IReadOnlySet<Guid>> FindFavoriteIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> pageIds,
        CancellationToken cancellationToken
    );

    // Keyset-paginated full-text search over title and block PlainText, scoped to pages in
    // notebooks the user owns or collaborates on, ordered by UpdatedAtUtc desc with Id tiebreak.
    Task<IReadOnlyList<PageSearchMatch>> SearchAsync(
        Guid userId,
        string query,
        DateTimeOffset? cursorUpdatedAtUtc,
        Guid? cursorId,
        int pageSize,
        CancellationToken cancellationToken
    );

    // The user's favorite pages, optionally narrowed to one notebook.
    Task<IReadOnlyList<Page>> ListFavoritesAsync(Guid userId, Guid? notebookId, CancellationToken cancellationToken);
}
