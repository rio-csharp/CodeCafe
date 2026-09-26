using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Pages.Abstractions;

public interface IPageRepository
{
    Task<Page?> FindByIdAsync(Guid pageId, CancellationToken cancellationToken);

    Task<Page?> FindBySlugAsync(Guid notebookId, string slug, CancellationToken cancellationToken);

    // Non-trashed pages of the notebook, ordered by SortKey; tree assembly happens in memory.
    Task<IReadOnlyList<Page>> ListByNotebookAsync(Guid notebookId, CancellationToken cancellationToken);

    // Non-trashed siblings under one parent (null = notebook root), ordered by SortKey.
    Task<IReadOnlyList<Page>> ListSiblingsAsync(Guid notebookId, Guid? parentId, CancellationToken cancellationToken);

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

    // The user's favorite pages, optionally narrowed to one notebook.
    Task<IReadOnlyList<Page>> ListFavoritesAsync(Guid userId, Guid? notebookId, CancellationToken cancellationToken);
}
