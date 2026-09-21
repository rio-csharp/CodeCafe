using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.ListNotebooks;
using CodeCafe.Domain.Notebooks;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Persistence;

public sealed class NotebookRepository(AppDbContext dbContext) : INotebookRepository
{
    public Task<Notebook?> FindBySlugAsync(string slug, CancellationToken cancellationToken)
        => dbContext.Notebooks.FirstOrDefaultAsync(notebook => notebook.Slug == slug, cancellationToken);

    public async Task<Notebook?> FindByIdOrSlugAsync(string idOrSlug, CancellationToken cancellationToken)
    {
        var query = dbContext.Notebooks.Include(notebook => notebook.Shares);
        return Guid.TryParse(idOrSlug, out var id)
            ? await query.FirstOrDefaultAsync(notebook => notebook.Id == id, cancellationToken)
            : await query.FirstOrDefaultAsync(
                notebook => notebook.Slug == idOrSlug.Trim().ToLowerInvariant(),
                cancellationToken
            );
    }

    public Task<int> CountVisibleAsync(
        Guid userId,
        string? tag,
        bool? isFavorite,
        NotebookVisibility? visibility,
        CancellationToken cancellationToken
    ) => VisibleTo(userId, tag, isFavorite, visibility).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Notebook>> ListVisibleAsync(
        Guid userId,
        string? tag,
        bool? isFavorite,
        NotebookVisibility? visibility,
        NotebookSort sort,
        int skip,
        int take,
        CancellationToken cancellationToken
    )
    {
        var query = VisibleTo(userId, tag, isFavorite, visibility);

        var ordered = sort switch
        {
            NotebookSort.TitleAsc => query.OrderBy(notebook => notebook.Title).ThenBy(notebook => notebook.Id),
            NotebookSort.CreatedDesc => query
                .OrderByDescending(notebook => notebook.CreatedAtUtc)
                .ThenByDescending(notebook => notebook.Id),
            _ => query.OrderByDescending(notebook => notebook.UpdatedAtUtc).ThenByDescending(notebook => notebook.Id),
        };

        return await ordered.Skip(skip).Take(take).ToListAsync(cancellationToken);
    }

    private IQueryable<Notebook> VisibleTo(Guid userId, string? tag, bool? isFavorite, NotebookVisibility? visibility)
    {
        var query = dbContext.Notebooks.Where(
            notebook => notebook.OwnerId == userId || notebook.Shares.Any(share => share.UserId == userId)
        );

        if (tag is not null)
        {
            query = query.Where(notebook => notebook.Tags.Contains(tag));
        }

        if (isFavorite is not null)
        {
            query = query.Where(notebook =>
                dbContext.NotebookFavorites.Any(
                    favorite => favorite.NotebookId == notebook.Id && favorite.UserId == userId
                ) == isFavorite.Value
            );
        }

        if (visibility is not null)
        {
            query = query.Where(notebook => notebook.Visibility == visibility.Value);
        }

        return query;
    }

    public async Task SetFavoriteAsync(Guid notebookId, Guid userId, bool isFavorite, CancellationToken cancellationToken)
    {
        var existing = await dbContext.NotebookFavorites.FindAsync([notebookId, userId], cancellationToken);
        if (isFavorite && existing is null)
        {
            dbContext.NotebookFavorites.Add(NotebookFavorite.Create(notebookId, userId));
        }
        else if (!isFavorite && existing is not null)
        {
            dbContext.NotebookFavorites.Remove(existing);
        }
    }

    public async Task<IReadOnlySet<Guid>> FindFavoriteIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> notebookIds,
        CancellationToken cancellationToken
    )
    {
        var ids = await dbContext.NotebookFavorites
            .Where(favorite => favorite.UserId == userId && notebookIds.Contains(favorite.NotebookId))
            .Select(favorite => favorite.NotebookId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public async Task AddAsync(Notebook notebook, CancellationToken cancellationToken)
        => await dbContext.Notebooks.AddAsync(notebook, cancellationToken);
}
