using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.AddNotebookFavorite;

public sealed class AddNotebookFavoriteCommandHandler(
    INotebookFavoriteService notebookFavoriteService)
    : ICommandHandler<AddNotebookFavoriteCommand, NotesResult<NotebookFavoriteModel>>
{
    public async Task<NotesResult<NotebookFavoriteModel>> Handle(
        AddNotebookFavoriteCommand request,
        CancellationToken cancellationToken)
    {
        return await notebookFavoriteService.AddFavoriteAsync(
            request.NotebookId,
            request.CurrentUserId,
            cancellationToken);
    }
}
