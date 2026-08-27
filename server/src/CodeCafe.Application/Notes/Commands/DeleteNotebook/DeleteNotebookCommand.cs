using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Commands.DeleteNotebook;

public sealed record DeleteNotebookCommand(Guid NotebookId, Guid CurrentUserId)
    : ICommand<NotesResult>;
