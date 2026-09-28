using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Notebooks.GetNotebookTree;

public sealed class GetNotebookTreeQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IPasswordHasher passwordHasher
) : IQueryHandler<GetNotebookTreeQuery, Result<NotebookTreeDto>>
{
    public async Task<Result<NotebookTreeDto>> Handle(GetNotebookTreeQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        var notebook = await notebooks.FindByIdOrSlugAsync(query.NotebookIdOrSlug, cancellationToken);
        if (notebook is null)
        {
            return Result.Failure<NotebookTreeDto>(NotebookErrors.NotFound);
        }

        var notebookError = NotebookAccess.CheckRead(notebook, userId, query.AccessCode, passwordHasher);
        var all = await pages.ListByNotebookAsync(notebook.Id, cancellationToken);

        IReadOnlyCollection<Page> visible;
        if (notebookError is null)
        {
            visible = all;
        }
        else if (userId is not null)
        {
            visible = SharedSubtreePages(all, userId.Value);
            if (visible.Count == 0)
            {
                return Result.Failure<NotebookTreeDto>(notebookError);
            }
        }
        else
        {
            return Result.Failure<NotebookTreeDto>(notebookError);
        }

        // One batched lookup marks the caller's favorites on the tree; anonymous viewers skip it.
        var favoriteIds = userId is not null
            ? await pages.FindFavoriteIdsAsync(userId.Value, visible.Select(page => page.Id).ToList(), cancellationToken)
            : new HashSet<Guid>();

        return Result.Success(new NotebookTreeDto(notebook.Id, BuildTree(all, visible, favoriteIds)));
    }

    // Page-share view: the shared pages plus their whole subtrees, nothing else.
    private static IReadOnlyCollection<Page> SharedSubtreePages(IReadOnlyList<Page> all, Guid userId)
    {
        var childrenOf = all.ToLookup(page => page.ParentId);
        var allowed = new Dictionary<Guid, Page>();
        var pending = new Queue<Page>(all.Where(page => page.IsSharedWith(userId)));
        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            if (!allowed.TryAdd(current.Id, current))
            {
                continue;
            }

            foreach (var child in childrenOf[current.Id])
            {
                pending.Enqueue(child);
            }
        }

        return allowed.Values.ToList();
    }

    private static IReadOnlyList<PageTreeNodeDto> BuildTree(
        IReadOnlyList<Page> all,
        IReadOnlyCollection<Page> visible,
        IReadOnlySet<Guid> favoriteIds
    )
    {
        // Paths walk the full chain, including ancestors hidden from a page-share viewer;
        // slugs carry no access, so they are not treated as secrets.
        var allById = all.ToDictionary(page => page.Id);
        var visibleById = visible.ToDictionary(page => page.Id);
        var childrenOf = visible.ToLookup(page => page.ParentId);

        // A node whose parent is invisible (or absent) surfaces as a root in this view.
        var roots = visible
            .Where(page => page.ParentId is null || !visibleById.ContainsKey(page.ParentId.Value))
            .OrderBy(page => page.SortKey);

        return BuildLevel(roots, childrenOf, allById, favoriteIds);
    }

    private static IReadOnlyList<PageTreeNodeDto> BuildLevel(
        IEnumerable<Page> siblings,
        ILookup<Guid?, Page> childrenOf,
        IReadOnlyDictionary<Guid, Page> allById,
        IReadOnlySet<Guid> favoriteIds
    ) => siblings
        .Select((page, index) => new PageTreeNodeDto(
            page.Id,
            page.Title,
            Path: PageHierarchy.PathOf(page, allById),
            SortOrder: index,
            page.IsArchived,
            favoriteIds.Contains(page.Id),
            BuildLevel(childrenOf[page.Id].OrderBy(child => child.SortKey), childrenOf, allById, favoriteIds)
        ))
        .ToList();

}
