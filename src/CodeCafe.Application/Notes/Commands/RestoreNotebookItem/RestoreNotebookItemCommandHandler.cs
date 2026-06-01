using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.RestoreNotebookItem;

public sealed class RestoreNotebookItemCommandHandler(
    INotebookCommandService notebookCommandService)
    : ICommandHandler<RestoreNotebookItemCommand, NotesResult<NotebookItemModel>>
{
    public async Task<NotesResult<NotebookItemModel>> Handle(
        RestoreNotebookItemCommand request,
        CancellationToken cancellationToken)
    {
        return await notebookCommandService.RestoreNotebookItemAsync(
            request.NotebookId,
            request.ItemId,
            request.CurrentUserId,
            cancellationToken);
    }
}
