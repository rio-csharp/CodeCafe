using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Trash.ListTrashedPages;

public sealed class ListTrashedPagesQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages
) : IQueryHandler<ListTrashedPagesQuery, Result<PagedResult<TrashedPageEntryDto>>>
{
    // Trash lists are owner-only, so a short list of sane bounds is enough.
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<PagedResult<TrashedPageEntryDto>>> Handle(
        ListTrashedPagesQuery query,
        CancellationToken cancellationToken
    )
    {
        var context = await NotebookAccess.RequireOwnerAsync(query.NotebookIdOrSlug, currentUserAccessor, notebooks, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure<PagedResult<TrashedPageEntryDto>>(error);
        }

        var trashed = await pages.ListTrashedByNotebookAsync(context.Value!.Notebook.Id, cancellationToken);
        var trashedIds = trashed.Select(page => page.Id).ToHashSet();
        var childrenOf = trashed.ToLookup(page => page.ParentId);

        // A trashed subtree shows as its topmost trashed page only; a member whose parent is
        // also trashed is reached through that root, so it is not listed on its own.
        var roots = trashed.Where(page => page.ParentId is null || !trashedIds.Contains(page.ParentId.Value)).ToList();

        var page = Math.Max(query.Page ?? 1, 1);
        var pageSize = Math.Clamp(query.PageSize ?? DefaultPageSize, 1, MaxPageSize);

        var items = roots
            .OrderByDescending(root => root.DeletedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(root => new TrashedPageEntryDto(
                root.Id,
                root.Title,
                root.Slug,
                SubtreeSize(root, childrenOf) - 1,
                root.DeletedAtUtc!.Value
            ))
            .ToList();

        return Result.Success(new PagedResult<TrashedPageEntryDto>(items, page, pageSize, roots.Count));
    }

    private static int SubtreeSize(Page root, ILookup<Guid?, Page> childrenOf)
    {
        var size = 0;
        var pending = new Queue<Page>([root]);
        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            size++;
            foreach (var child in childrenOf[current.Id])
            {
                pending.Enqueue(child);
            }
        }

        return size;
    }
}
