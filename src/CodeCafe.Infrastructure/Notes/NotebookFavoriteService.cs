using CodeCafe.Application.Notes;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Notes;

public sealed class NotebookFavoriteService(ApplicationDbContext dbContext) : INotebookFavoriteService
{
    public async Task<NotesResult<NotebookFavoriteModel>> GetFavoriteStatusAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var notebook = await dbContext.Notebooks
            .AsNoTracking()
            .SingleOrDefaultAsync(existingNotebook => existingNotebook.Id == notebookId, cancellationToken);
        if (notebook is null)
        {
            return NotesResult<NotebookFavoriteModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
        }

        if (!NotesSupport.CanReadNotebook(notebook, currentUserId))
        {
            return NotesResult<NotebookFavoriteModel>.Failure(NotesFailureKind.Forbidden, "notebook_forbidden", "You do not have access to this notebook.");
        }

        return NotesResult<NotebookFavoriteModel>.Success(await BuildFavoriteModelAsync(notebookId, currentUserId, cancellationToken));
    }

    public async Task<NotesResult<NotebookFavoriteModel>> AddFavoriteAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var notebook = await dbContext.Notebooks
            .SingleOrDefaultAsync(existingNotebook => existingNotebook.Id == notebookId, cancellationToken);
        if (notebook is null)
        {
            return NotesResult<NotebookFavoriteModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
        }

        if (!NotesSupport.CanReadNotebook(notebook, currentUserId))
        {
            return NotesResult<NotebookFavoriteModel>.Failure(NotesFailureKind.Forbidden, "notebook_forbidden", "You do not have access to this notebook.");
        }

        var existingFavorite = await dbContext.NotebookFavorites.SingleOrDefaultAsync(
            favorite => favorite.NotebookId == notebookId && favorite.UserId == currentUserId,
            cancellationToken);
        if (existingFavorite is null)
        {
            var favoriteEntry = dbContext.NotebookFavorites.Add(new Domain.Notes.NotebookFavorite
            {
                Id = Guid.NewGuid(),
                NotebookId = notebookId,
                UserId = currentUserId
            });
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (NotesSupport.IsDuplicateFavoriteException(exception))
            {
                favoriteEntry.State = EntityState.Detached;
            }
        }

        return NotesResult<NotebookFavoriteModel>.Success(await BuildFavoriteModelAsync(notebookId, currentUserId, cancellationToken));
    }

    public async Task<NotesResult<NotebookFavoriteModel>> RemoveFavoriteAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var notebook = await dbContext.Notebooks
            .SingleOrDefaultAsync(existingNotebook => existingNotebook.Id == notebookId, cancellationToken);
        if (notebook is null)
        {
            return NotesResult<NotebookFavoriteModel>.Failure(NotesFailureKind.NotFound, "notebook_not_found", "Notebook was not found.");
        }

        if (!NotesSupport.CanReadNotebook(notebook, currentUserId))
        {
            return NotesResult<NotebookFavoriteModel>.Failure(NotesFailureKind.Forbidden, "notebook_forbidden", "You do not have access to this notebook.");
        }

        var existingFavorite = await dbContext.NotebookFavorites.SingleOrDefaultAsync(
            favorite => favorite.NotebookId == notebookId && favorite.UserId == currentUserId,
            cancellationToken);
        if (existingFavorite is not null)
        {
            dbContext.NotebookFavorites.Remove(existingFavorite);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return NotesResult<NotebookFavoriteModel>.Success(await BuildFavoriteModelAsync(notebookId, currentUserId, cancellationToken));
    }

    private async Task<NotebookFavoriteModel> BuildFavoriteModelAsync(
        Guid notebookId,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var favoriteCount = await dbContext.NotebookFavorites
            .AsNoTracking()
            .CountAsync(favorite => favorite.NotebookId == notebookId, cancellationToken);
        var isFavorited = currentUserId != Guid.Empty && await dbContext.NotebookFavorites
            .AsNoTracking()
            .AnyAsync(favorite => favorite.NotebookId == notebookId && favorite.UserId == currentUserId, cancellationToken);

        return new NotebookFavoriteModel(notebookId, isFavorited, favoriteCount);
    }
}
