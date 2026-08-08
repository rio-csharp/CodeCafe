using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Commands.RestoreNotebookItem;

public sealed class RestoreNotebookItemCommandHandler(
    INotebookItemMutationService notebookItemMutationService)
    : ICommandHandler<RestoreNotebookItemCommand, NotesResult<NotebookItemModel>>
{
    public async Task<NotesResult<NotebookItemModel>> Handle(
        RestoreNotebookItemCommand request,
        CancellationToken cancellationToken)
    {
        return await notebookItemMutationService.RestoreNotebookItemAsync(
            request.NotebookId,
            request.ItemId,
            request.CurrentUserId,
            cancellationToken);
    }
}
