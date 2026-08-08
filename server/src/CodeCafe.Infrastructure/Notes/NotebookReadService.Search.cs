using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Notes;

public sealed partial class NotebookReadService
{
    public async Task<IReadOnlyList<NotebookItemSearchModel>> SearchVisibleNotebookItemsAsync(
        Guid currentUserId,
        string search,
        CancellationToken cancellationToken,
        int? limit = null)
    {
        var normalizedSearch = NotebookInput.NormalizeSearch(search);
        if (normalizedSearch is null)
        {
            return [];
        }

        var query = dbContext.NotebookItems
            .AsNoTracking()
            .Where(item => !item.IsArchived)
            .Where(item =>
                item.Notebook.OwnerId == currentUserId
                || (item.Notebook.Visibility == NotebookVisibility.Public && item.Notebook.IsPublished));

        query = ApplyItemSearch(query, normalizedSearch);

        return await ApplyLimit(
                query.OrderBy(item => item.Notebook.Title)
                    .ThenBy(item => item.NotebookId)
                    .ThenBy(item => item.Path),
                limit)
            .Select(item => new NotebookItemSearchModel(
                item.NotebookId,
                item.Notebook.Slug,
                item.Notebook.Title,
                item.Notebook.OwnerId == currentUserId,
                item.Id,
                item.Path,
                item.Title,
                item.Type.ToString().ToLowerInvariant(),
                item.PlainTextContent,
                item.CreatedAtUtc,
                item.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Notebook> ApplyNotebookSearch(IQueryable<Notebook> query, string normalizedSearch)
    {
        return UsesPostgresProvider()
            ? query.Where(notebook =>
                EF.Functions.ILike(notebook.Title, normalizedSearch)
                || (notebook.Description != null && EF.Functions.ILike(notebook.Description, normalizedSearch)))
            : query.Where(notebook =>
                EF.Functions.Like(notebook.Title.ToLower(), normalizedSearch.ToLower())
                || (notebook.Description != null
                    && EF.Functions.Like(notebook.Description.ToLower(), normalizedSearch.ToLower())));
    }

    private IQueryable<NotebookItem> ApplyItemSearch(IQueryable<NotebookItem> query, string normalizedSearch)
    {
        return UsesPostgresProvider()
            ? query.Where(item =>
                EF.Functions.ILike(item.Title, normalizedSearch)
                || (item.PlainTextContent != null && EF.Functions.ILike(item.PlainTextContent, normalizedSearch)))
            : query.Where(item =>
                EF.Functions.Like(item.Title.ToLower(), normalizedSearch.ToLower())
                || (item.PlainTextContent != null
                    && EF.Functions.Like(item.PlainTextContent.ToLower(), normalizedSearch.ToLower())));
    }
}
