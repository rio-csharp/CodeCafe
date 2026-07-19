using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Commands.DeleteNotebookItem;

public sealed record DeleteNotebookItemCommand(
    Guid NotebookId,
    Guid ItemId,
    Guid CurrentUserId) : ICommand<NotesResult>;
