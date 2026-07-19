using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Commands.UpdateNotebook;

public sealed record UpdateNotebookCommand(
    Guid NotebookId,
    Guid CurrentUserId,
    string Title,
    string? Description,
    string Visibility) : ICommand<NotesResult<NotebookDetailModel>>;
