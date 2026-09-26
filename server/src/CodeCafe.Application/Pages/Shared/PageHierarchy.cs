using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Pages.Shared;

// Helpers for the parent chain: paths are derived, so every read that needs a path or an
// ancestor-scoped share check walks up from the page.
internal static class PageHierarchy
{
    // Root-first ancestors of the page. A trashed or purged parent ends the walk, and the
    // visited set guards against a corrupted cycle.
    public static async Task<IReadOnlyList<Page>> LoadAncestorsAsync(
        Page page,
        IPageRepository pages,
        CancellationToken cancellationToken
    )
    {
        var ancestors = new List<Page>();
        var visited = new HashSet<Guid> { page.Id };
        var current = page;

        while (current.ParentId is { } parentId && visited.Add(parentId))
        {
            var parent = await pages.FindByIdAsync(parentId, cancellationToken);
            if (parent is null)
            {
                break;
            }

            ancestors.Insert(0, parent);
            current = parent;
        }

        return ancestors;
    }

    public static string PathOf(Page page, IReadOnlyList<Page> ancestors)
        => PagePath.Build(ancestors.Select(ancestor => ancestor.Slug).Append(page.Slug));

    // In-memory variant for callers that already hold every page of the notebook. The visited
    // set guards against a corrupted cycle.
    public static string PathOf(Page page, IReadOnlyDictionary<Guid, Page> allById)
    {
        var segments = new List<string>();
        var current = page;
        var visited = new HashSet<Guid>();
        while (visited.Add(current.Id))
        {
            segments.Insert(0, current.Slug);
            if (current.ParentId is not { } parentId || !allById.TryGetValue(parentId, out var parent))
            {
                break;
            }

            current = parent;
        }

        return PagePath.Build(segments);
    }

    // The last segment names the page (slugs are unique per notebook); the prefix is verified
    // against the ancestor chain so "/a/b" never resolves to a "b" that lives elsewhere.
    public static async Task<Page?> ResolveByPathAsync(
        Guid notebookId,
        string path,
        IPageRepository pages,
        CancellationToken cancellationToken
    )
    {
        var segments = PagePath.Parse(path);
        if (segments.Count == 0)
        {
            return null;
        }

        var page = await pages.FindBySlugAsync(notebookId, segments[^1], cancellationToken);
        if (page is null)
        {
            return null;
        }

        var ancestors = await LoadAncestorsAsync(page, pages, cancellationToken);
        var prefix = ancestors.Select(ancestor => ancestor.Slug).ToList();

        return prefix.SequenceEqual(segments.Take(segments.Count - 1)) ? page : null;
    }
}
