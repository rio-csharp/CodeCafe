using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.UpdateNotebook;

public sealed class UpdateNotebookCommandHandler(
    INotebookCommandService notebookCommandService)
    : ICommandHandler<UpdateNotebookCommand, NotesResult<NotebookDetailModel>>
{
    public async Task<NotesResult<NotebookDetailModel>> Handle(
        UpdateNotebookCommand request,
        CancellationToken cancellationToken)
    {
        return await notebookCommandService.UpdateNotebookAsync(
            request.NotebookId,
            request.CurrentUserId,
            request.Title,
            request.Description,
            request.Visibility,
            cancellationToken);
    }
}
