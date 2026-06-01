using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.DeleteNotebook;

public sealed class DeleteNotebookCommandHandler(
    INotebookCommandService notebookCommandService)
    : ICommandHandler<DeleteNotebookCommand, NotesResult>
{
    public async Task<NotesResult> Handle(
        DeleteNotebookCommand request,
        CancellationToken cancellationToken)
    {
        return await notebookCommandService.DeleteNotebookAsync(
            request.NotebookId,
            request.CurrentUserId,
            cancellationToken);
    }
}
