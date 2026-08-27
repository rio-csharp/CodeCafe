using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Commands.RestoreNotebookItem;

public sealed record RestoreNotebookItemCommand(Guid NotebookId, Guid ItemId, Guid CurrentUserId)
    : ICommand<NotesResult<NotebookItemModel>>;
