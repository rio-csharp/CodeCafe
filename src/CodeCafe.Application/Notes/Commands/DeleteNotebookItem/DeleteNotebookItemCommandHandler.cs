using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.DeleteNotebookItem;

public sealed class DeleteNotebookItemCommandHandler(
    INotebookItemMutationService notebookItemMutationService)
    : ICommandHandler<DeleteNotebookItemCommand, NotesResult>
{
    public async Task<NotesResult> Handle(
        DeleteNotebookItemCommand request,
        CancellationToken cancellationToken)
    {
        return await notebookItemMutationService.DeleteNotebookItemAsync(
            request.NotebookId,
            request.ItemId,
            request.CurrentUserId,
            cancellationToken);
    }
}
