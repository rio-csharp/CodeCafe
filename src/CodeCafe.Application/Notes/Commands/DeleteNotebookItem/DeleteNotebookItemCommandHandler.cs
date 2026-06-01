using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.DeleteNotebookItem;

public sealed class DeleteNotebookItemCommandHandler(
    INotebookCommandService notebookCommandService)
    : ICommandHandler<DeleteNotebookItemCommand, NotesResult>
{
    public async Task<NotesResult> Handle(
        DeleteNotebookItemCommand request,
        CancellationToken cancellationToken)
    {
        return await notebookCommandService.DeleteNotebookItemAsync(
            request.NotebookId,
            request.ItemId,
            request.CurrentUserId,
            cancellationToken);
    }
}
