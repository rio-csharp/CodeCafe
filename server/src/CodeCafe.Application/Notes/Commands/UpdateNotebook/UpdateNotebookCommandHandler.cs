using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Commands.UpdateNotebook;

public sealed class UpdateNotebookCommandHandler(
    INotebookMutationStore notebookMutationStore,
    INotebookReadService notebookReadService,
    IDateTimeProvider dateTimeProvider
) : ICommandHandler<UpdateNotebookCommand, NotesResult<NotebookDetailModel>>
{
    public async Task<NotesResult<NotebookDetailModel>> Handle(
        UpdateNotebookCommand request,
        CancellationToken cancellationToken
    )
    {
        if (!NotebookInput.TryParseVisibility(request.Visibility, out var parsedVisibility))
        {
            return NotesResult<NotebookDetailModel>.Failure(
                NotesFailureKind.Validation,
                "invalid_visibility",
                "Visibility must be public, private, or unlisted."
            );
        }

        var notebook = await notebookMutationStore.GetOwnedNotebookAsync(
            request.NotebookId,
            request.CurrentUserId,
            cancellationToken
        );
        if (notebook is null)
        {
            return await notebookMutationStore.NotebookExistsAsync(
                request.NotebookId,
                cancellationToken
            )
                ? NotesResult<NotebookDetailModel>.Failure(
                    NotesFailureKind.Forbidden,
                    "notebook_forbidden",
                    "Only the notebook owner can modify it."
                )
                : NotesResult<NotebookDetailModel>.Failure(
                    NotesFailureKind.NotFound,
                    "notebook_not_found",
                    "Notebook was not found."
                );
        }

        var trimmedTitle = request.Title.Trim();
        var titleChanged = !string.Equals(notebook.Title, trimmedTitle, StringComparison.Ordinal);
        notebook.Rename(trimmedTitle);
        if (titleChanged)
        {
            notebook.Slug = await notebookMutationStore.GenerateUniqueNotebookSlugAsync(
                trimmedTitle,
                notebook.Id,
                cancellationToken
            );
        }

        notebook.SetDescription(NotebookInput.NormalizeOptionalText(request.Description));
        notebook.ApplyVisibility(parsedVisibility, dateTimeProvider.UtcNow);

        await notebookMutationStore.SaveNotebookAsync(notebook, trimmedTitle, cancellationToken);

        return await notebookReadService.GetNotebookByIdAsync(
            notebook.Id,
            request.CurrentUserId,
            cancellationToken
        );
    }
}
