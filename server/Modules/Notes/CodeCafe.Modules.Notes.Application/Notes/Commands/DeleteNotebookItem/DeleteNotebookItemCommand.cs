using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.DeleteNotebookItem;

public sealed record DeleteNotebookItemCommand(
    Guid NotebookId,
    Guid ItemId,
    Guid CurrentUserId) : ICommand<NotesResult>;
