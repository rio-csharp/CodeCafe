using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.CreateNotebook;

public sealed record CreateNotebookCommand(
    Guid CurrentUserId,
    string Title,
    string? Description,
    string? Visibility) : ICommand<NotesResult<NotebookDetailModel>>;
