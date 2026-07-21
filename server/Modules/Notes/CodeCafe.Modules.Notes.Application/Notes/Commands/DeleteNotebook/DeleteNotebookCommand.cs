using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Commands.DeleteNotebook;

public sealed record DeleteNotebookCommand(
    Guid NotebookId,
    Guid CurrentUserId) : ICommand<NotesResult>;
