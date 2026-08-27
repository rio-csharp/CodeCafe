using CodeCafe.Application.Common.Messaging;
using CodeCafe.Domain.Notes;

namespace CodeCafe.Application.Notes.Commands.AddNotebookFavorite;

public sealed class AddNotebookFavoriteCommandHandler(INotebookMutationStore notebookMutationStore)
    : ICommandHandler<AddNotebookFavoriteCommand, NotesResult<NotebookFavoriteModel>>
{
    public async Task<NotesResult<NotebookFavoriteModel>> Handle(
        AddNotebookFavoriteCommand request,
        CancellationToken cancellationToken
    )
    {
        var notebook = await notebookMutationStore.GetNotebookAsync(
            request.NotebookId,
            cancellationToken
        );
        if (notebook is null)
        {
            return NotesResult<NotebookFavoriteModel>.Failure(
                NotesFailureKind.NotFound,
                "notebook_not_found",
                "Notebook was not found."
            );
        }

        if (!NotebookAccessPolicy.CanReadNotebook(notebook, request.CurrentUserId))
        {
            return NotesResult<NotebookFavoriteModel>.Failure(
                NotesFailureKind.Forbidden,
                "notebook_forbidden",
                "You do not have access to this notebook."
            );
        }

        var existingFavorite = await notebookMutationStore.GetFavoriteAsync(
            request.NotebookId,
            request.CurrentUserId,
            cancellationToken
        );
        if (existingFavorite is null)
        {
            try
            {
                await notebookMutationStore.AddFavoriteAsync(
                    new NotebookFavorite
                    {
                        Id = Guid.NewGuid(),
                        NotebookId = request.NotebookId,
                        UserId = request.CurrentUserId,
                    },
                    cancellationToken
                );
            }
            catch (Exception exception) when (IsDuplicateFavoriteException(exception))
            {
                // Concurrent double-favorite resolved: the favorite already exists, so map to 409
                // instead of 500. The client can refetch to see the current state.
                return NotesResult<NotebookFavoriteModel>.Failure(
                    NotesFailureKind.Conflict,
                    "favorite_already_exists",
                    "The notebook is already favorited by this user."
                );
            }
        }

        return await BuildFavoriteResultAsync(
            notebookMutationStore,
            request.NotebookId,
            request.CurrentUserId,
            cancellationToken
        );
    }

    private static bool IsDuplicateFavoriteException(Exception exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("NotebookFavorites", StringComparison.OrdinalIgnoreCase)
            && (
                message.Contains("unique", StringComparison.OrdinalIgnoreCase)
                || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            );
    }

    private static async Task<NotesResult<NotebookFavoriteModel>> BuildFavoriteResultAsync(
        INotebookMutationStore notebookMutationStore,
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken
    )
    {
        var favoriteCount = await notebookMutationStore.CountFavoritesAsync(
            notebookId,
            cancellationToken
        );
        var isFavorited =
            currentUserId != Guid.Empty
            && await notebookMutationStore.IsFavoritedAsync(
                notebookId,
                currentUserId,
                cancellationToken
            );

        return NotesResult<NotebookFavoriteModel>.Success(
            new NotebookFavoriteModel(notebookId, isFavorited, favoriteCount)
        );
    }
}
