using CodeCafe.Application.Notes;
using System.Text.Json;

namespace CodeCafe.Infrastructure.Notes;

public sealed class NotebookCommandService(
    INotebookItemMutationService notebookItemMutationService) : INotebookCommandService
{
    public Task<NotesResult<NotebookItemModel>> CreateNotebookItemAsync(
        Guid notebookId,
        Guid currentUserId,
        Guid? parentId,
        string type,
        string title,
        int sortOrder,
        JsonElement? contentJson,
        CancellationToken cancellationToken)
        => notebookItemMutationService.CreateNotebookItemAsync(
            notebookId,
            currentUserId,
            parentId,
            type,
            title,
            sortOrder,
            contentJson,
            cancellationToken);

    public Task<NotesResult<NotebookItemModel>> UpdateNotebookItemAsync(
        Guid notebookId,
        Guid itemId,
        Guid currentUserId,
        string title,
        JsonElement parentId,
        int? sortOrder,
        JsonElement contentJson,
        CancellationToken cancellationToken,
        DateTimeOffset? expectedUpdatedAtUtc = null)
        => notebookItemMutationService.UpdateNotebookItemAsync(
            notebookId,
            itemId,
            currentUserId,
            title,
            parentId,
            sortOrder,
            contentJson,
            cancellationToken,
            expectedUpdatedAtUtc);

    public Task<NotesResult<IReadOnlyList<NotebookItemModel>>> ReorderNotebookItemsAsync(
        Guid notebookId,
        Guid currentUserId,
        IReadOnlyList<ReorderNotebookItemModel> items,
        CancellationToken cancellationToken)
        => notebookItemMutationService.ReorderNotebookItemsAsync(
            notebookId,
            currentUserId,
            items,
            cancellationToken);

    public Task<NotesResult> DeleteNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
        => notebookItemMutationService.DeleteNotebookItemAsync(notebookId, itemId, currentUserId, cancellationToken);

    public Task<NotesResult<NotebookItemModel>> ArchiveNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
        => notebookItemMutationService.ArchiveNotebookItemAsync(notebookId, itemId, currentUserId, cancellationToken);

    public Task<NotesResult<NotebookItemModel>> RestoreNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken)
        => notebookItemMutationService.RestoreNotebookItemAsync(notebookId, itemId, currentUserId, cancellationToken);
}
