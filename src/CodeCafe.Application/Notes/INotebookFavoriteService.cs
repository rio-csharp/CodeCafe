namespace CodeCafe.Application.Notes;

public interface INotebookFavoriteService
{
    Task<NotesResult<NotebookFavoriteModel>> GetFavoriteStatusAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken);

    Task<NotesResult<NotebookFavoriteModel>> AddFavoriteAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken);

    Task<NotesResult<NotebookFavoriteModel>> RemoveFavoriteAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken);
}
