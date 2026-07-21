using CodeCafe.Modules.Notes.Domain.Notes;

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

    void AddFavorite(NotebookFavorite favorite);

    void RemoveFavorite(NotebookFavorite favorite);

    Task<int> CountFavoritesAsync(Guid notebookId, CancellationToken cancellationToken);

    Task<bool> IsFavoritedAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
