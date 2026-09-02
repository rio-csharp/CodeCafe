using CodeCafe.Application.Common.Messaging;
using CodeCafe.Domain.Notes;
using CodeCafe.Domain.Notes.ValueObjects;

namespace CodeCafe.Application.Notes.Commands.CreateNotebook;

public sealed class CreateNotebookCommandHandler(
    INotebookRepository notebooks,
    INotebookSlugGenerator slugGenerator,
    INotebookReadService readService,
    TimeProvider timeProvider
) : ICommandHandler<CreateNotebookCommand, NotesResult<NotebookDetailModel>>
{
    public async Task<NotesResult<NotebookDetailModel>> Handle(
        CreateNotebookCommand request,
        CancellationToken cancellationToken
    )
    {
        if (!NotebookInput.TryParseVisibility(request.Visibility, out var visibility))
        {
            return NotesResult<NotebookDetailModel>.Failure(
                NotesFailureKind.Validation,
                "invalid_visibility",
                "Visibility must be public, private, or unlisted."
            );
        }

        var title = request.Title.Trim();
        var requestedSlug = NotebookInput.NormalizeOptionalText(request.Slug);

        NotebookSlug slug;
        if (requestedSlug is null)
        {
            slug = await slugGenerator.GenerateUniqueSlugAsync(title, null, cancellationToken);
        }
        else
        {
            if (!NotebookInput.IsValidSlug(requestedSlug))
            {
                return NotesResult<NotebookDetailModel>.Failure(
                    NotesFailureKind.Validation,
                    "invalid_slug",
                    "Slug must be 8-180 characters of lowercase letters, digits, and dashes."
                );
            }

            slug = NotebookSlug.Create(requestedSlug);
            // An explicitly requested slug is a contract: report the conflict instead of silently suffixing it.
            if (!await slugGenerator.IsSlugAvailableAsync(slug, null, cancellationToken))
            {
                return NotesResult<NotebookDetailModel>.Failure(
                    NotesFailureKind.Conflict,
                    "slug_taken",
                    "A notebook with this slug already exists."
                );
            }
        }

        var notebook = Notebook.Create(
            Guid.CreateVersion7(),
            request.UserId,
            title,
            slug,
            NotebookInput.NormalizeOptionalText(request.Description),
            visibility,
            timeProvider.GetUtcNow()
        );

        notebooks.Add(notebook);
        await notebooks.SaveChangesAsync(cancellationToken);

        return await readService.GetNotebookByIdAsync(notebook.Id, request.UserId, cancellationToken);
    }
}
