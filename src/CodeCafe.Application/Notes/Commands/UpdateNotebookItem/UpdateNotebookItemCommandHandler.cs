using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.UpdateNotebookItem;

public sealed class UpdateNotebookItemCommandHandler(
    INotebookCommandService notebookCommandService)
    : ICommandHandler<UpdateNotebookItemCommand, NotesResult<NotebookItemModel>>
{
    public async Task<NotesResult<NotebookItemModel>> Handle(
        UpdateNotebookItemCommand request,
        CancellationToken cancellationToken)
    {
        return await notebookCommandService.UpdateNotebookItemAsync(
            request.NotebookId,
            request.ItemId,
            request.CurrentUserId,
            request.Title,
            request.ParentId,
            request.SortOrder,
            request.ContentJson,
            cancellationToken,
            request.ExpectedUpdatedAtUtc);
    }
}
