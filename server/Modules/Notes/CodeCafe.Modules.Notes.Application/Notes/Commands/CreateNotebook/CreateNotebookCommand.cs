using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Commands.CreateNotebook;

public sealed record CreateNotebookCommand(
    Guid CurrentUserId,
    string Title,
    string? Description,
    string? Visibility) : ICommand<NotesResult<NotebookDetailModel>>;
