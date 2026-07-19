using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Commands.UpdateNotebookItem;

public sealed class UpdateNotebookItemCommandHandler(
    INotebookItemMutationService notebookItemMutationService)
    : ICommandHandler<UpdateNotebookItemCommand, NotesResult<NotebookItemModel>>
{
    public async Task<NotesResult<NotebookItemModel>> Handle(
        UpdateNotebookItemCommand request,
        CancellationToken cancellationToken)
    {
        return await notebookItemMutationService.UpdateNotebookItemAsync(
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
