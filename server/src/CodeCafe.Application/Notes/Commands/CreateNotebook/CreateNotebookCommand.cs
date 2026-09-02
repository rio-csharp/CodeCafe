using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Commands.CreateNotebook;

public sealed record CreateNotebookCommand(
    Guid UserId,
    string Title,
    string? Description,
    string? Visibility,
    string? Slug = null
) : ICommand<NotesResult<NotebookDetailModel>>;
