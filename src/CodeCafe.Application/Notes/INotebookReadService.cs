namespace CodeCafe.Application.Notes;

public interface INotebookReadService
{
    Task<IReadOnlyList<NotebookSummaryModel>> GetPublicNotebooksAsync(
        string? search,
        Guid currentUserId,
        CancellationToken cancellationToken,
        int? limit = null);

    Task<IReadOnlyList<NotebookSummaryModel>> GetMyNotebooksAsync(
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        int? limit = null);

    Task<IReadOnlyList<NotebookItemSearchModel>> SearchVisibleNotebookItemsAsync(
        Guid currentUserId,
        string search,
        CancellationToken cancellationToken,
        int? limit = null);

    Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(string slug, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false);

    Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetPublicNotebookItemsAsync(string slug, CancellationToken cancellationToken);

    Task<NotesResult<NotebookItemModel>> GetPublicNotebookItemAsync(string slug, string path, CancellationToken cancellationToken);

    Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false);

    Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(string slug, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false);

    Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetNotebookItemsAsync(
        Guid notebookId,
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        int? limit = null);
}
