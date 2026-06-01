using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.ReorderNotebookItems;

public sealed class ReorderNotebookItemsCommandHandler(
    INotebookCommandService notebookCommandService)
    : ICommandHandler<ReorderNotebookItemsCommand, NotesResult<IReadOnlyList<NotebookItemModel>>>
{
    public async Task<NotesResult<IReadOnlyList<NotebookItemModel>>> Handle(
        ReorderNotebookItemsCommand request,
        CancellationToken cancellationToken)
    {
        return await notebookCommandService.ReorderNotebookItemsAsync(
            request.NotebookId,
            request.CurrentUserId,
            request.Items,
            cancellationToken);
    }
}
