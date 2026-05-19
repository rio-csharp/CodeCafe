using System.Text.Json;

namespace CodeCafe.Application.Notes;

public interface INotebookCommandService
{
    Task<NotesResult<NotebookDetailModel>> CreateNotebookAsync(
        Guid currentUserId,
        string title,
        string? description,
        string? visibility,
        CancellationToken cancellationToken);

    Task<NotesResult<NotebookDetailModel>> UpdateNotebookAsync(
        Guid notebookId,
        Guid currentUserId,
        string title,
        string? description,
        string? visibility,
        CancellationToken cancellationToken);

    Task<NotesResult> DeleteNotebookAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken);

    Task<NotesResult<NotebookItemModel>> CreateNotebookItemAsync(
        Guid notebookId,
        Guid currentUserId,
        Guid? parentId,
        string type,
        string title,
        int sortOrder,
        JsonElement? contentJson,
        string? plainTextContent,
        CancellationToken cancellationToken);

    Task<NotesResult<NotebookItemModel>> UpdateNotebookItemAsync(
        Guid notebookId,
        Guid itemId,
        Guid currentUserId,
        string title,
        JsonElement parentId,
        int sortOrder,
        JsonElement? contentJson,
        string? plainTextContent,
        CancellationToken cancellationToken);

    Task<NotesResult<IReadOnlyList<NotebookItemModel>>> ReorderNotebookItemsAsync(
        Guid notebookId,
        Guid currentUserId,
        IReadOnlyList<ReorderNotebookItemModel> items,
        CancellationToken cancellationToken);

    Task<NotesResult> DeleteNotebookItemAsync(Guid notebookId, Guid itemId, Guid currentUserId, CancellationToken cancellationToken);
}
