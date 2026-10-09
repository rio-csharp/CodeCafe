using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Domain.Pages;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Persistence;

public sealed class PageRepository(AppDbContext dbContext) : IPageRepository
{
    public async Task<Page?> FindByIdAsync(Guid pageId, CancellationToken cancellationToken)
        => await dbContext.Pages
            .Include(page => page.Shares)
            .FirstOrDefaultAsync(page => page.Id == pageId, cancellationToken);

    public async Task LockNotebookForPageStructureAsync(
        Guid pageId,
        bool includeTrashed,
        CancellationToken cancellationToken)
    {
        if (includeTrashed)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""SELECT 1 FROM notebooks WHERE "Id" = (SELECT "NotebookId" FROM pages WHERE "Id" = {pageId}) AND "DeletedAtUtc" IS NULL FOR UPDATE""",
                cancellationToken);
            dbContext.DetachUnchangedPageStructureEntities();
            return;
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""SELECT 1 FROM notebooks WHERE "Id" = (SELECT "NotebookId" FROM pages WHERE "Id" = {pageId} AND "DeletedAtUtc" IS NULL) AND "DeletedAtUtc" IS NULL FOR UPDATE""",
            cancellationToken);
        dbContext.DetachUnchangedPageStructureEntities();
    }

    public async Task<Page?> FindBySlugAsync(Guid notebookId, string slug, CancellationToken cancellationToken)
        => await dbContext.Pages
            .Include(page => page.Shares)
            .FirstOrDefaultAsync(page => page.NotebookId == notebookId && page.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Page>> ListByNotebookAsync(Guid notebookId, CancellationToken cancellationToken)
        => await dbContext.Pages
            .Include(page => page.Shares)
            .Where(page => page.NotebookId == notebookId)
            .OrderBy(page => page.SortKey)
            .ToListAsync(cancellationToken);

    public async Task<Page?> FindTrashedByIdAsync(Guid pageId, CancellationToken cancellationToken)
        => await dbContext.Pages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(page => page.Id == pageId && page.DeletedAtUtc != null, cancellationToken);

    public async Task<IReadOnlyList<Page>> ListTrashedByNotebookAsync(Guid notebookId, CancellationToken cancellationToken)
        => await dbContext.Pages
            .IgnoreQueryFilters()
            .Where(page => page.NotebookId == notebookId && page.DeletedAtUtc != null)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PageSearchMatch>> SearchAsync(
        Guid userId,
        string query,
        DateTimeOffset? cursorUpdatedAtUtc,
        Guid? cursorId,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var pattern = LikePatterns.Substring(query);

        // Visibility mirrors the owned + notebook-share arms of NotebookRepository.VisibleTo; the
        // twin predicates stay in sync by hand because sharing an EF expression tree across the
        // two repositories is not worth the coupling. Page-share subtrees are a deliberate v1
        // exclusion. The global query filters keep trashed pages and notebooks out.
        var matches =
            from page in dbContext.Pages
            join notebook in dbContext.Notebooks on page.NotebookId equals notebook.Id
            where notebook.OwnerId == userId || notebook.Shares.Any(share => share.UserId == userId)
            where EF.Functions.ILike(page.Title, pattern, LikePatterns.EscapeCharacter)
                || dbContext.Blocks.Any(
                    block => block.PageId == page.Id && EF.Functions.ILike(block.PlainText, pattern, LikePatterns.EscapeCharacter)
                )
            select new { page, notebook };

        if (cursorUpdatedAtUtc is { } updatedAtUtc && cursorId is { } id)
        {
            // Keyset on (UpdatedAtUtc desc, Id desc): PostgreSQL uuid supports plain comparison,
            // so the tiebreak translates to SQL.
            matches = matches.Where(match =>
                match.page.UpdatedAtUtc < updatedAtUtc
                || (match.page.UpdatedAtUtc == updatedAtUtc && match.page.Id.CompareTo(id) < 0)
            );
        }

        return await matches
            .OrderByDescending(match => match.page.UpdatedAtUtc)
            .ThenByDescending(match => match.page.Id)
            .Take(pageSize)
            .Select(match => new PageSearchMatch(
                match.page,
                match.notebook.Title,
                dbContext.Blocks
                    .Where(block => block.PageId == match.page.Id && EF.Functions.ILike(block.PlainText, pattern, LikePatterns.EscapeCharacter))
                    .OrderBy(block => block.SortKey)
                    .Select(block => (string?)block.PlainText)
                    .FirstOrDefault()
            ))
            .ToListAsync(cancellationToken);
    }

    // Favorites and shares cascade with the page row (PageFavoriteConfiguration, PageConfiguration).
    public void Remove(Page page) => dbContext.Pages.Remove(page);

    public async Task<IReadOnlyList<Page>> ListChildrenAsync(Guid notebookId, Guid? parentId, CancellationToken cancellationToken)
        => await dbContext.Pages
            .Where(page => page.NotebookId == notebookId && page.ParentId == parentId)
            .OrderBy(page => page.SortKey)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Page page, CancellationToken cancellationToken)
        => await dbContext.Pages.AddAsync(page, cancellationToken);

    public async Task<int> CountByNotebookAsync(Guid notebookId, CancellationToken cancellationToken)
        => await dbContext.Pages.CountAsync(page => page.NotebookId == notebookId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> CountByNotebooksAsync(
        IReadOnlyCollection<Guid> notebookIds,
        CancellationToken cancellationToken
    ) => await dbContext.Pages
        .Where(page => notebookIds.Contains(page.NotebookId))
        .GroupBy(page => page.NotebookId)
        .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);

    public async Task SetFavoriteAsync(Guid pageId, Guid userId, bool isFavorite, CancellationToken cancellationToken)
    {
        var existing = await dbContext.PageFavorites.FindAsync([pageId, userId], cancellationToken);
        if (isFavorite && existing is null)
        {
            dbContext.PageFavorites.Add(PageFavorite.Create(pageId, userId));
        }
        else if (!isFavorite && existing is not null)
        {
            dbContext.PageFavorites.Remove(existing);
        }
    }

    public async Task<IReadOnlySet<Guid>> FindFavoriteIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> pageIds,
        CancellationToken cancellationToken
    )
    {
        var ids = await dbContext.PageFavorites
            .Where(favorite => favorite.UserId == userId && pageIds.Contains(favorite.PageId))
            .Select(favorite => favorite.PageId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public async Task<IReadOnlyList<Page>> ListFavoritesAsync(Guid userId, Guid? notebookId, CancellationToken cancellationToken)
    {
        var query = dbContext.PageFavorites
            .Where(favorite => favorite.UserId == userId)
            .Join(dbContext.Pages, favorite => favorite.PageId, page => page.Id, (_, page) => page);
        if (notebookId is not null)
        {
            query = query.Where(page => page.NotebookId == notebookId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
