using CodeCafe.Application.Notebooks.ListNotebooks;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Notebooks.Abstractions;

public interface INotebookRepository
{
    Task<Notebook?> FindBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<Notebook?> FindByIdOrSlugAsync(string idOrSlug, CancellationToken cancellationToken);

    Task<int> CountVisibleAsync(
        Guid userId,
        string? tag,
        bool? isFavorite,
        NotebookVisibility? visibility,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<Notebook>> ListVisibleAsync(
        Guid userId,
        string? tag,
        bool? isFavorite,
        NotebookVisibility? visibility,
        NotebookSort sort,
        int skip,
        int take,
        CancellationToken cancellationToken
    );

    Task AddAsync(Notebook notebook, CancellationToken cancellationToken);

    // Favorites are per-user; setting one that already exists (or clearing an absent one) is a no-op.
    Task SetFavoriteAsync(Guid notebookId, Guid userId, bool isFavorite, CancellationToken cancellationToken);

    Task<IReadOnlySet<Guid>> FindFavoriteIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> notebookIds,
        CancellationToken cancellationToken
    );
}
