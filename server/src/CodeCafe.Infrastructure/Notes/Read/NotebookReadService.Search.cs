using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using CodeCafe.Domain.Notes.Enums;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Notes.Read;

public sealed partial class NotebookReadService
{
    public async Task<IReadOnlyList<NotebookItemSearchModel>> SearchVisibleNotebookItemsAsync(
        Guid currentUserId,
        string search,
        CancellationToken cancellationToken,
        int? limit = null
    )
    {
        var normalizedSearch = NotebookInput.NormalizeSearch(search);
        if (normalizedSearch is null)
        {
            return [];
        }

        var items = ApplyItemSearch(
            dbContext.NotebookItems.AsNoTracking().Where(item => !item.IsArchived),
            normalizedSearch
        );

        var visibleNotebooks = dbContext
            .Notebooks.AsNoTracking()
            .Where(NotebookAccessPredicates.ReadableByUser(
                dbContext, currentUserId, includeUnlisted: false));

        var term = search.Trim();

        var query =
            from item in items
            join notebook in visibleNotebooks
                on item.NotebookId equals notebook.Id
            orderby notebook.Title, item.NotebookId, item.Path
            select new
            {
                NotebookId = notebook.Id,
                NotebookSlug = notebook.Slug,
                NotebookTitle = notebook.Title,
                NotebookOwnerId = notebook.OwnerId,
                ItemId = item.Id,
                item.Path,
                item.Title,
                item.Type,
                // Title-only matches miss the window (IndexOf = -1) and fall back to the page head.
                Snippet =
                    item.PlainTextContent == null
                        ? null
                        : item.PlainTextContent.Substring(
                            Math.Max(
                                item.PlainTextContent.ToLower().IndexOf(term.ToLower())
                                    - SnippetLeadChars,
                                0
                            ),
                            SnippetLength
                        ),
                item.CreatedAtUtc,
                item.UpdatedAtUtc,
            };

        var rows = await ApplyLimit(query, limit).ToListAsync(cancellationToken);

        return rows
            .Select(row => new NotebookItemSearchModel(
                row.NotebookId,
                row.NotebookSlug.Value,
                row.NotebookTitle,
                row.NotebookOwnerId == currentUserId,
                row.ItemId,
                row.Path.Value,
                row.Title,
                row.Type.ToString().ToLowerInvariant(),
                row.Snippet,
                row.CreatedAtUtc,
                row.UpdatedAtUtc
            ))
            .ToList();
    }

    private const int SnippetLeadChars = 100;
    private const int SnippetLength = 400;

    private IQueryable<Notebook> ApplyNotebookSearch(
        IQueryable<Notebook> query,
        string normalizedSearch
    )
    {
        return query.Where(notebook =>
                EF.Functions.ILike(notebook.Title, normalizedSearch)
                || (
                    notebook.Description != null
                    && EF.Functions.ILike(notebook.Description, normalizedSearch)
                )
        );
    }

    private IQueryable<NotebookItem> ApplyItemSearch(
        IQueryable<NotebookItem> query,
        string normalizedSearch
    )
    {
        return query.Where(item =>
                EF.Functions.ILike(item.Title, normalizedSearch)
                || (
                    item.PlainTextContent != null
                    && EF.Functions.ILike(item.PlainTextContent, normalizedSearch)
                )
        );
    }
}
