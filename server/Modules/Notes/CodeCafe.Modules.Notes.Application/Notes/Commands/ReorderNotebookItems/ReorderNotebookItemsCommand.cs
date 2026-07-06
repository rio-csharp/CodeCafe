using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.ReorderNotebookItems;

public sealed record ReorderNotebookItemsCommand(
    Guid NotebookId,
    Guid CurrentUserId,
    IReadOnlyList<ReorderNotebookItemModel> Items) : ICommand<NotesResult<IReadOnlyList<NotebookItemModel>>>;
