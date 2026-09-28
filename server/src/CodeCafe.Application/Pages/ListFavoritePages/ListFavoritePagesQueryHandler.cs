using CodeCafe.Application.Auth;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;

namespace CodeCafe.Application.Pages.ListFavoritePages;

public sealed class ListFavoritePagesQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages
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
        var titlesByNotebook = (await notebooks.FindByIdsAsync(
                favorites.Select(page => page.NotebookId).Distinct().ToList(),
                cancellationToken
            ))
            .ToDictionary(notebook => notebook.Id, notebook => notebook.Title);

        var dtos = favorites
            .Where(page => titlesByNotebook.ContainsKey(page.NotebookId))
            .Select(page => new FavoritePageDto(page.Id, page.Title, page.NotebookId, titlesByNotebook[page.NotebookId]))
            .OrderBy(dto => dto.NotebookTitle, StringComparer.OrdinalIgnoreCase)
            .ThenBy(dto => dto.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result.Success<IReadOnlyList<FavoritePageDto>>(dtos);
    }
}
