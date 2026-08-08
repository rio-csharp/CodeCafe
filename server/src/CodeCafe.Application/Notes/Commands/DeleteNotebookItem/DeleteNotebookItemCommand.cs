using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Commands.DeleteNotebookItem;

public sealed record DeleteNotebookItemCommand(
    Guid NotebookId,
    Guid ItemId,
    Guid CurrentUserId) : ICommand<NotesResult>;
