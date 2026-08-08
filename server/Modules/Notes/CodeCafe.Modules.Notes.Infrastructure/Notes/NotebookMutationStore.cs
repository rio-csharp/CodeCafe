using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Modules.Notes.Domain.Notes;
using CodeCafe.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Modules.Notes.Infrastructure.Notes;

public sealed class NotebookMutationStore(ApplicationDbContext dbContext) : INotebookMutationStore
{
    public void AddNotebook(Notebook notebook)
    {
        dbContext.Notebooks.Add(notebook);
    }

    public async Task<NotebookFavorite?> GetFavoriteAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
    {
        return await dbContext.NotebookFavorites.SingleOrDefaultAsync(
            favorite => favorite.NotebookId == notebookId && favorite.UserId == currentUserId,
            cancellationToken);
    }

    public async Task<Notebook?> GetNotebookAsync(Guid notebookId, CancellationToken cancellationToken)
    {
        return await dbContext.Notebooks.SingleOrDefaultAsync(
            notebook => notebook.Id == notebookId,
            cancellationToken);
    }

    public async Task<Notebook?> GetOwnedNotebookAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
    {
        return await dbContext.Notebooks.SingleOrDefaultAsync(
            notebook => notebook.Id == notebookId && notebook.OwnerId == currentUserId,
            cancellationToken);
    }

    public async Task<bool> NotebookExistsAsync(Guid notebookId, CancellationToken cancellationToken)
    {
        return await dbContext.Notebooks.AnyAsync(notebook => notebook.Id == notebookId, cancellationToken);
    }

    public async Task<string> GenerateUniqueNotebookSlugAsync(
        string title,
        Guid? currentNotebookId,
        CancellationToken cancellationToken)
    {
        var baseSlug = NotebookSlugGenerator.FromTitle(title, "note");
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var slug = NotebookSlugGenerator.WithSuffix(baseSlug, attempt);
            var exists = await dbContext.Notebooks.AnyAsync(
                notebook => notebook.Slug == slug && notebook.Id != currentNotebookId,
                cancellationToken);
            if (!exists)
            {
                return slug;
            }
        }

        return NotebookSlugGenerator.WithUniqueSuffix(baseSlug, NotebookSlugGenerator.MaxSlugLength);
    }

    public void RemoveNotebook(Notebook notebook)
    {
        dbContext.Notebooks.Remove(notebook);
    }

    public async Task SaveNotebookAsync(Notebook notebook, string title, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException exception) when (NotesSupport.IsDuplicateNotebookSlugException(exception) && attempt < 4)
            {
                notebook.Slug = await GenerateUniqueNotebookSlugAsync(title, notebook.Id, cancellationToken);
                if (dbContext.Entry(notebook).State == EntityState.Modified)
                {
                    dbContext.Entry(notebook).Property(existingNotebook => existingNotebook.Slug).IsModified = true;
                }
            }
        }
    }

    public async Task<int> CountFavoritesAsync(Guid notebookId, CancellationToken cancellationToken)
    {
        return await dbContext.NotebookFavorites
            .AsNoTracking()
            .CountAsync(favorite => favorite.NotebookId == notebookId, cancellationToken);
    }

    public async Task<bool> IsFavoritedAsync(Guid notebookId, Guid currentUserId, CancellationToken cancellationToken)
    {
        return await dbContext.NotebookFavorites
            .AsNoTracking()
            .AnyAsync(favorite => favorite.NotebookId == notebookId && favorite.UserId == currentUserId, cancellationToken);
    }

    public async Task AddFavoriteAsync(NotebookFavorite favorite, CancellationToken cancellationToken)
    {
        dbContext.NotebookFavorites.Add(favorite);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (NotesSupport.IsDuplicateFavoriteException(exception))
        {
            // A concurrent request (double-clicked favorite button) won the race against the
            // check-then-insert. The row exists, which is what the caller wanted, so detach the
            // losing insert and report success.
            dbContext.Entry(favorite).State = EntityState.Detached;
        }
    }

    public void RemoveFavorite(NotebookFavorite favorite)
    {
        dbContext.NotebookFavorites.Remove(favorite);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
