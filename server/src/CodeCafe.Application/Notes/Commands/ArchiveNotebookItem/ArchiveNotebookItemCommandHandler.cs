using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notes.Commands.ArchiveNotebookItem;

public sealed class ArchiveNotebookItemCommandHandler(
    INotebookItemMutationService notebookItemMutationService)
    : ICommandHandler<ArchiveNotebookItemCommand, NotesResult<NotebookItemModel>>
{
    public async Task<NotesResult<NotebookItemModel>> Handle(
        ArchiveNotebookItemCommand request,
        CancellationToken cancellationToken)
    {
        return await notebookItemMutationService.ArchiveNotebookItemAsync(
            request.NotebookId,
            request.ItemId,
            request.CurrentUserId,
            cancellationToken);
    }
}
