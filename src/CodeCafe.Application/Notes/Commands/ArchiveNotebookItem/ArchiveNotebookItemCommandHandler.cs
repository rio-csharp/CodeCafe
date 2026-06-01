using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.ArchiveNotebookItem;

public sealed class ArchiveNotebookItemCommandHandler(
    INotebookCommandService notebookCommandService)
    : ICommandHandler<ArchiveNotebookItemCommand, NotesResult<NotebookItemModel>>
{
    public async Task<NotesResult<NotebookItemModel>> Handle(
        ArchiveNotebookItemCommand request,
        CancellationToken cancellationToken)
    {
        return await notebookCommandService.ArchiveNotebookItemAsync(
            request.NotebookId,
            request.ItemId,
            request.CurrentUserId,
            cancellationToken);
    }
}
