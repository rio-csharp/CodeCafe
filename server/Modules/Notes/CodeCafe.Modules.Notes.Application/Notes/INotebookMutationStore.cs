using CodeCafe.Domain.Notes;

namespace CodeCafe.Modules.Notes.Application.Notes;

public interface INotebookMutationStore
{
    Task<Notebook?> GetOwnedNotebookAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken);

    Task<Notebook?> GetNotebookAsync(Guid notebookId, CancellationToken cancellationToken);

    Task<bool> NotebookExistsAsync(Guid notebookId, CancellationToken cancellationToken);

    Task<string> GenerateUniqueNotebookSlugAsync(
        string title,
        Guid? currentNotebookId,
        CancellationToken cancellationToken);

    void AddNotebook(Notebook notebook);

    Task SaveNotebookAsync(Notebook notebook, string title, CancellationToken cancellationToken);

    void RemoveNotebook(Notebook notebook);

    Task<NotebookFavorite?> GetFavoriteAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a favorite and persists it. Favoriting is idempotent: a concurrent request that already
    /// inserted the same (NotebookId, UserId) pair is absorbed rather than surfaced as an error,
    /// because the caller's desired end state is already satisfied.
    /// </summary>
    Task AddFavoriteAsync(NotebookFavorite favorite, CancellationToken cancellationToken);

    void RemoveFavorite(NotebookFavorite favorite);

    Task<int> CountFavoritesAsync(Guid notebookId, CancellationToken cancellationToken);

    Task<bool> IsFavoritedAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
