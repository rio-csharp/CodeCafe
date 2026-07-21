using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Commands.CreateNotebookItem;

public sealed class CreateNotebookItemCommandHandler(
    INotebookItemMutationService notebookItemMutationService)
    : ICommandHandler<CreateNotebookItemCommand, NotesResult<NotebookItemModel>>
{
    public async Task<NotesResult<NotebookItemModel>> Handle(
        CreateNotebookItemCommand request,
        CancellationToken cancellationToken)
    {
        return await notebookItemMutationService.CreateNotebookItemAsync(
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
