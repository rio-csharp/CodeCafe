using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Notebooks.Abstractions;

public interface INotebookRepository
{
    Task<Notebook?> FindBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<Notebook?> FindByIdAsync(Guid notebookId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Notebook>> FindByIdsAsync(IReadOnlyCollection<Guid> notebookIds, CancellationToken cancellationToken);

    Task<Notebook?> FindByIdOrSlugAsync(string idOrSlug, CancellationToken cancellationToken);

    // A null userId means anonymous: only public notebooks are visible.
    Task<int> CountVisibleAsync(Guid? userId, NotebookFilter filter, CancellationToken cancellationToken);

    Task<IReadOnlyList<Notebook>> ListVisibleAsync(
        Guid? userId,
        NotebookFilter filter,
        NotebookSort sort,
        int skip,
        int take,
        CancellationToken cancellationToken
    );

    Task AddAsync(Notebook notebook, CancellationToken cancellationToken);

    // Trash operations must see past the soft-delete query filter.
    Task<Notebook?> FindTrashedByIdAsync(Guid notebookId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Notebook>> ListTrashedAsync(Guid ownerId, int skip, int take, CancellationToken cancellationToken);

    Task<int> CountTrashedAsync(Guid ownerId, CancellationToken cancellationToken);

    // Hard delete, for purging from the trash; dependents cascade.
    void Remove(Notebook notebook);

    // Favorites are per-user; setting one that already exists (or clearing an absent one) is a no-op.
    Task SetFavoriteAsync(Guid notebookId, Guid userId, bool isFavorite, CancellationToken cancellationToken);

    Task<IReadOnlySet<Guid>> FindFavoriteIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> notebookIds,
        CancellationToken cancellationToken
    );
}
