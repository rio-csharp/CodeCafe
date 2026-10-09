using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.ListFavoritePages;

public sealed class ListFavoritePagesQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IPasswordHasher passwordHasher
) : IQueryHandler<ListFavoritePagesQuery, Result<IReadOnlyList<FavoritePageDto>>>
{
    public async Task<Result<IReadOnlyList<FavoritePageDto>>> Handle(
        ListFavoritePagesQuery query,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUserAccessor.User?.Id;
        if (userId is null)
        {
            return Result.Failure<IReadOnlyList<FavoritePageDto>>(AuthErrors.UserNotFound);
        }

        var favorites = await pages.ListFavoritesAsync(userId.Value, query.NotebookId, cancellationToken);

        // One IN query, not one lookup per notebook; trashed notebooks are filtered out,
        // which is also what hides their pages' favorites below.
        var notebooksById = (await notebooks.FindByIdsAsync(
                favorites.Select(page => page.NotebookId).Distinct().ToList(),
                cancellationToken
            ))
            .ToDictionary(notebook => notebook.Id);

        var visible = new List<Domain.Pages.Page>(favorites.Count);
        var pagesByNotebook = new Dictionary<Guid, IReadOnlyDictionary<Guid, Domain.Pages.Page>>();
        foreach (var page in favorites)
        {
            if (!notebooksById.TryGetValue(page.NotebookId, out var notebook))
            {
                continue;
            }

            if (NotebookAccess.CheckRead(notebook, userId, accessCode: null, passwordHasher) is null)
            {
                visible.Add(page);
                continue;
            }

            if (!pagesByNotebook.TryGetValue(notebook.Id, out var notebookPages))
            {
                notebookPages = (await pages.ListByNotebookAsync(notebook.Id, cancellationToken))
                    .ToDictionary(candidate => candidate.Id);
                pagesByNotebook.Add(notebook.Id, notebookPages);
            }

            var ancestors = AncestorsOf(page, notebookPages);
            if (PageAccess.CheckRead(notebook, page, ancestors, userId, accessCode: null, passwordHasher) is null)
            {
                visible.Add(page);
            }
        }

        var dtos = visible
            .Select(page => new FavoritePageDto(page.Id, page.Title, page.NotebookId, notebooksById[page.NotebookId].Title))
            .OrderBy(dto => dto.NotebookTitle, StringComparer.OrdinalIgnoreCase)
            .ThenBy(dto => dto.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result.Success<IReadOnlyList<FavoritePageDto>>(dtos);
    }

    private static IReadOnlyList<Domain.Pages.Page> AncestorsOf(
        Domain.Pages.Page page,
        IReadOnlyDictionary<Guid, Domain.Pages.Page> pagesById)
    {
        var ancestors = new List<Domain.Pages.Page>();
        var visited = new HashSet<Guid> { page.Id };
        var current = page;
        while (current.ParentId is { } parentId
            && visited.Add(parentId)
            && pagesById.TryGetValue(parentId, out var parent))
        {
            ancestors.Insert(0, parent);
            current = parent;
        }

        return ancestors;
    }
}
