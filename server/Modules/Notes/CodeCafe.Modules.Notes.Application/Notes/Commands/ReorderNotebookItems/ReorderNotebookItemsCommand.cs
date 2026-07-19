using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Commands.ReorderNotebookItems;

public sealed record ReorderNotebookItemsCommand(
    Guid NotebookId,
    Guid CurrentUserId,
    IReadOnlyList<ReorderNotebookItemModel> Items) : ICommand<NotesResult<IReadOnlyList<NotebookItemModel>>>;
