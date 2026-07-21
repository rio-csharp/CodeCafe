using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Notes.Application.Notes.Queries.GetNotebookFavoriteStatus;

public sealed class GetNotebookFavoriteStatusQueryHandler(
    INotebookMutationStore notebookMutationStore)
    : IQueryHandler<GetNotebookFavoriteStatusQuery, NotesResult<NotebookFavoriteModel>>
{
    public async Task<NotesResult<NotebookFavoriteModel>> Handle(
        GetNotebookFavoriteStatusQuery request,
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

        var favoriteCount = await notebookMutationStore.CountFavoritesAsync(request.NotebookId, cancellationToken);
        var isFavorited = request.CurrentUserId != Guid.Empty
            && await notebookMutationStore.IsFavoritedAsync(request.NotebookId, request.CurrentUserId, cancellationToken);

        return NotesResult<NotebookFavoriteModel>.Success(
            new NotebookFavoriteModel(request.NotebookId, isFavorited, favoriteCount));
    }
}
