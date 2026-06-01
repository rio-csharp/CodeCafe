using CodeCafe.Application.Notes;

namespace CodeCafe.Infrastructure.Notes;

public sealed class NotebookQueryService(
    INotebookReadService notebookReadService) : INotebookQueryService
{
    public Task<IReadOnlyList<NotebookSummaryModel>> GetPublicNotebooksAsync(
        string? search,
        Guid currentUserId,
        CancellationToken cancellationToken,
        int? limit = null)
        => notebookReadService.GetPublicNotebooksAsync(search, currentUserId, cancellationToken, limit);

    public Task<IReadOnlyList<NotebookSummaryModel>> GetMyNotebooksAsync(
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        int? limit = null)
        => notebookReadService.GetMyNotebooksAsync(currentUserId, search, cancellationToken, limit);

    public Task<IReadOnlyList<NotebookItemSearchModel>> SearchVisibleNotebookItemsAsync(
        Guid currentUserId,
        string search,
        CancellationToken cancellationToken,
        int? limit = null)
        => notebookReadService.SearchVisibleNotebookItemsAsync(currentUserId, search, cancellationToken, limit);

    public Task<NotesResult<NotebookDetailModel>> GetPublicNotebookAsync(string slug, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
        => notebookReadService.GetPublicNotebookAsync(slug, currentUserId, cancellationToken, includeArchived);

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetPublicNotebookItemsAsync(string slug, CancellationToken cancellationToken)
        => notebookReadService.GetPublicNotebookItemsAsync(slug, cancellationToken);

    public Task<NotesResult<NotebookItemModel>> GetPublicNotebookItemAsync(string slug, string path, CancellationToken cancellationToken)
        => notebookReadService.GetPublicNotebookItemAsync(slug, path, cancellationToken);

    public Task<NotesResult<NotebookDetailModel>> GetNotebookByIdAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
        => notebookReadService.GetNotebookByIdAsync(notebookId, currentUserId, cancellationToken, includeArchived);

    public Task<NotesResult<NotebookDetailModel>> GetNotebookBySlugAsync(string slug, Guid currentUserId, CancellationToken cancellationToken, bool includeArchived = false)
        => notebookReadService.GetNotebookBySlugAsync(slug, currentUserId, cancellationToken, includeArchived);

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> GetNotebookItemsAsync(
        Guid notebookId,
        Guid currentUserId,
        string? search,
        CancellationToken cancellationToken,
        bool includeArchived = false,
        int? limit = null)
        => notebookReadService.GetNotebookItemsAsync(notebookId, currentUserId, search, cancellationToken, includeArchived, limit);
}
