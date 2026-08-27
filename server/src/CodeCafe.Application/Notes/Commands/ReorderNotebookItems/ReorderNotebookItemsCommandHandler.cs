using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Commands.ReorderNotebookItems;

public sealed class ReorderNotebookItemsCommandHandler(
    INotebookItemMutationService notebookItemMutationService
) : ICommandHandler<ReorderNotebookItemsCommand, NotesResult<IReadOnlyList<NotebookItemModel>>>
{
    public async Task<NotesResult<IReadOnlyList<NotebookItemModel>>> Handle(
        ReorderNotebookItemsCommand request,
        CancellationToken cancellationToken
    )
    {
        return await notebookItemMutationService.ReorderNotebookItemsAsync(
            request.NotebookId,
            request.CurrentUserId,
            request.Items,
            cancellationToken
        );
    }
}
