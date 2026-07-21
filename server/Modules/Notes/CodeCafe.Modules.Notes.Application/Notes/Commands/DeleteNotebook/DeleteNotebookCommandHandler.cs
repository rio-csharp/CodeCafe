using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Commands.DeleteNotebook;

public sealed class DeleteNotebookCommandHandler(
    INotebookMutationStore notebookMutationStore)
    : ICommandHandler<DeleteNotebookCommand, NotesResult>
{
    public async Task<NotesResult> Handle(
        DeleteNotebookCommand request,
        CancellationToken cancellationToken)
    {
        var notebook = await notebookMutationStore.GetOwnedNotebookAsync(
            request.NotebookId,
            request.CurrentUserId,
            cancellationToken);
        if (notebook is null)
        {
            return await notebookMutationStore.NotebookExistsAsync(request.NotebookId, cancellationToken)
                ? NotesResult.Failure(NotesFailureKind.Forbidden, "notebook_forbidden", "Only the notebook owner can delete it.")
                : NotesResult.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
        }

        notebookMutationStore.RemoveNotebook(notebook);
        await notebookMutationStore.SaveChangesAsync(cancellationToken);
        return NotesResult.Success();
    }
}
