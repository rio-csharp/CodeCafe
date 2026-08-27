using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Domain.Notes;

namespace CodeCafe.Application.Notes.Commands.CreateNotebook;

public sealed class CreateNotebookCommandHandler(
    INotebookMutationStore notebookMutationStore,
    INotebookReadService notebookReadService,
    IDateTimeProvider dateTimeProvider
) : ICommandHandler<CreateNotebookCommand, NotesResult<NotebookDetailModel>>
{
    public async Task<NotesResult<NotebookDetailModel>> Handle(
        CreateNotebookCommand request,
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

        var trimmedTitle = request.Title.Trim();
        var notebook = new Notebook
        {
            Id = Guid.NewGuid(),
            OwnerId = request.CurrentUserId,
            Title = trimmedTitle,
            Slug = await notebookMutationStore.GenerateUniqueNotebookSlugAsync(
                trimmedTitle,
                null,
                cancellationToken
            ),
            Description = NotebookInput.NormalizeOptionalText(request.Description),
        };
        notebook.ApplyVisibility(parsedVisibility, dateTimeProvider.UtcNow);

        notebookMutationStore.AddNotebook(notebook);
        await notebookMutationStore.SaveNotebookAsync(notebook, trimmedTitle, cancellationToken);

        return await notebookReadService.GetNotebookByIdAsync(
            notebook.Id,
            request.CurrentUserId,
            cancellationToken
        );
    }
}
