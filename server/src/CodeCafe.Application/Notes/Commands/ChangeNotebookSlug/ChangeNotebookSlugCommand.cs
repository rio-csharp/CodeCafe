using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Commands.ChangeNotebookSlug;

public sealed record ChangeNotebookSlugCommand(
    Guid UserId,
    Guid NotebookId,
    string Slug
) : ICommand<NotesResult<NotebookDetailModel>>;
