using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.UpdateNotebook;

public sealed record UpdateNotebookCommand(
    Guid NotebookId,
    Guid CurrentUserId,
    string Title,
    string? Description,
    string Visibility) : ICommand<NotesResult<NotebookDetailModel>>;
