using CodeCafe.Application.Common.Messaging;
using CodeCafe.Domain.Notes.ValueObjects;

namespace CodeCafe.Application.Notes.Commands.ChangeNotebookSlug;

public sealed class ChangeNotebookSlugCommandHandler(
    INotebookRepository notebooks,
    INotebookSlugGenerator slugGenerator,
    INotebookReadService readService
) : ICommandHandler<ChangeNotebookSlugCommand, NotesResult<NotebookDetailModel>>
{
    public async Task<NotesResult<NotebookDetailModel>> Handle(
        ChangeNotebookSlugCommand request,
        CancellationToken cancellationToken
    )
    {
        var notebook = await notebooks.FindByIdAsync(request.NotebookId, cancellationToken);
        if (notebook is null)
        {
            return NotesResult<NotebookDetailModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_not_found",
                "Notebook not found."
            );
        }

        if (notebook.OwnerId != request.UserId)
        {
            return NotesResult<NotebookDetailModel>.Failure(
                NotesFailureKind.Forbidden,
                "notebook_forbidden",
                "Only the owner can change the slug."
            );
        }

        if (!NotebookInput.IsValidSlug(request.Slug))
        {
            return NotesResult<NotebookDetailModel>.Failure(
                NotesFailureKind.Validation,
                "invalid_slug",
                "Slug must be 8-180 characters of lowercase letters, digits, and dashes."
            );
        }

        var slug = NotebookSlug.Create(request.Slug);
        if (notebook.Slug == slug)
        {
            return await readService.GetNotebookByIdAsync(notebook.Id, request.UserId, cancellationToken);
        }

        if (!await slugGenerator.IsSlugAvailableAsync(slug, notebook.Id, cancellationToken))
        {
            return NotesResult<NotebookDetailModel>.Failure(
                NotesFailureKind.Conflict,
                "slug_taken",
                "A notebook with this slug already exists."
            );
        }

        notebook.ChangeSlug(slug);
        await notebooks.SaveChangesAsync(cancellationToken);

        return await readService.GetNotebookByIdAsync(notebook.Id, request.UserId, cancellationToken);
    }
}
