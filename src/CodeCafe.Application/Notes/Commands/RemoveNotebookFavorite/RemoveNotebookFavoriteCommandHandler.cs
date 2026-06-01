using CodeCafe.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Application.Notes.Commands.RemoveNotebookFavorite;

public sealed class RemoveNotebookFavoriteCommandHandler(
    INotebookFavoriteService notebookFavoriteService)
    : ICommandHandler<RemoveNotebookFavoriteCommand, NotesResult<NotebookFavoriteModel>>
{
    public async Task<NotesResult<NotebookFavoriteModel>> Handle(
        RemoveNotebookFavoriteCommand request,
        CancellationToken cancellationToken)
    {
        return await notebookFavoriteService.RemoveFavoriteAsync(
            request.NotebookId,
            request.CurrentUserId,
            cancellationToken);
    }
}
