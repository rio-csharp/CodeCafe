using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.CreateNotebook;

public sealed class CreateNotebookCommandHandler(
    INotebookCommandService notebookCommandService)
    : ICommandHandler<CreateNotebookCommand, NotesResult<NotebookDetailModel>>
{
    public async Task<NotesResult<NotebookDetailModel>> Handle(
        CreateNotebookCommand request,
        CancellationToken cancellationToken)
    {
        return await notebookCommandService.CreateNotebookAsync(
            request.CurrentUserId,
            request.Title,
            request.Description,
            request.Visibility,
            cancellationToken);
    }
}
