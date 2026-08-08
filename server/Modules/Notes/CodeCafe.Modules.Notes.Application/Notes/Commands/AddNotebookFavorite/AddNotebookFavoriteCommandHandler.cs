using CodeCafe.Modules.Notes.Domain.Notes;
using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Commands.AddNotebookFavorite;

public sealed class AddNotebookFavoriteCommandHandler(
    INotebookMutationStore notebookMutationStore)
    : ICommandHandler<AddNotebookFavoriteCommand, NotesResult<NotebookFavoriteModel>>
{
    public async Task<NotesResult<NotebookFavoriteModel>> Handle(
        AddNotebookFavoriteCommand request,
        CancellationToken cancellationToken)
    {
        var notebook = await notebookMutationStore.GetNotebookAsync(request.NotebookId, cancellationToken);
        if (notebook is null)
        {
            return NotesResult<NotebookFavoriteModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
        }

        if (!NotebookAccessPolicy.CanReadNotebook(notebook, request.CurrentUserId))
        {
            return NotesResult<NotebookFavoriteModel>.Failure(NotesFailureKind.Forbidden, "notebook_forbidden", "You do not have access to this notebook.");
        }

        var existingFavorite = await notebookMutationStore.GetFavoriteAsync(
            request.NotebookId,
            request.CurrentUserId,
            cancellationToken);
        if (existingFavorite is null)
        {
            await notebookMutationStore.AddFavoriteAsync(
                new NotebookFavorite
                {
                    Id = Guid.NewGuid(),
                    NotebookId = request.NotebookId,
                    UserId = request.CurrentUserId
                },
                cancellationToken);
        }

        return await BuildFavoriteResultAsync(notebookMutationStore, request.NotebookId, request.CurrentUserId, cancellationToken);
    }

    private static async Task<NotesResult<NotebookFavoriteModel>> BuildFavoriteResultAsync(
        INotebookMutationStore notebookMutationStore,
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var favoriteCount = await notebookMutationStore.CountFavoritesAsync(notebookId, cancellationToken);
        var isFavorited = currentUserId != Guid.Empty
            && await notebookMutationStore.IsFavoritedAsync(notebookId, currentUserId, cancellationToken);

        return NotesResult<NotebookFavoriteModel>.Success(new NotebookFavoriteModel(notebookId, isFavorited, favoriteCount));
    }
}
