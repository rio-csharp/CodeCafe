using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
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

            var ancestors = PageHierarchy.AncestorsOf(
                page,
                await PagesOfAsync(notebook.Id, pagesByNotebook, cancellationToken)
            );
            if (PageAccess.CheckRead(notebook, page, ancestors, userId, accessCode: null, passwordHasher) is null)
            {
                visible.Add(page);
            }
        }

        // Paths need the ancestor chain even when the notebook-level check already granted
        // access, so the per-notebook page map doubles as the cache for both.
        var dtos = new List<FavoritePageDto>(visible.Count);
        foreach (var page in visible)
        {
            var notebookPages = await PagesOfAsync(page.NotebookId, pagesByNotebook, cancellationToken);
            var notebook = notebooksById[page.NotebookId];
            dtos.Add(new FavoritePageDto(
                page.Id,
                page.Title,
                PageHierarchy.PathOf(page, notebookPages),
                page.NotebookId,
                notebook.Title,
                notebook.Slug
            ));
        }

        var ordered = dtos
            .OrderBy(dto => dto.NotebookTitle, StringComparer.OrdinalIgnoreCase)
            .ThenBy(dto => dto.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result.Success<IReadOnlyList<FavoritePageDto>>(ordered);
    }

    // The per-notebook page map is needed twice — for the ancestor-scoped share check and again
    // for paths — so it is loaded at most once per notebook and cached for the rest of the read.
    private async Task<IReadOnlyDictionary<Guid, Domain.Pages.Page>> PagesOfAsync(
        Guid notebookId,
        Dictionary<Guid, IReadOnlyDictionary<Guid, Domain.Pages.Page>> cache,
        CancellationToken cancellationToken
    )
    {
        if (cache.TryGetValue(notebookId, out var cached))
        {
            return cached;
        }

        var loaded = (await pages.ListByNotebookAsync(notebookId, cancellationToken))
            .ToDictionary(candidate => candidate.Id);
        cache.Add(notebookId, loaded);
        return loaded;
    }
}
