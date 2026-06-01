using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.CreateNotebookItem;

public sealed class CreateNotebookItemCommandHandler(
    INotebookCommandService notebookCommandService)
    : ICommandHandler<CreateNotebookItemCommand, NotesResult<NotebookItemModel>>
{
    public async Task<NotesResult<NotebookItemModel>> Handle(
        CreateNotebookItemCommand request,
        CancellationToken cancellationToken)
    {
        return await notebookCommandService.CreateNotebookItemAsync(
            request.NotebookId,
            request.CurrentUserId,
            request.ParentId,
            request.Type,
            request.Title,
            request.SortOrder,
            request.ContentJson,
            cancellationToken);
    }
}
