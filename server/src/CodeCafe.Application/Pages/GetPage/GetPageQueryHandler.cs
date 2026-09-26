using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.GetPage;

public sealed class GetPageQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IUserRepository users,
    IPasswordHasher passwordHasher
) : IQueryHandler<GetPageQuery, Result<PageDetailsDto>>
{
    public async Task<Result<PageDetailsDto>> Handle(GetPageQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        var page = await pages.FindByIdAsync(query.PageId, cancellationToken);
        if (page is null)
        {
            return Result.Failure<PageDetailsDto>(PageErrors.NotFound);
        }

        var notebook = await notebooks.FindByIdAsync(page.NotebookId, cancellationToken);
        var ancestors = await PageHierarchy.LoadAncestorsAsync(page, pages, cancellationToken);

        var error = notebook is null
            ? PageErrors.NotFound
            : PageAccess.CheckRead(notebook, page, ancestors, userId, query.AccessCode, passwordHasher);
        if (error is not null)
        {
            return Result.Failure<PageDetailsDto>(error);
        }

        var isFavorite = userId is not null
            && (await pages.FindFavoriteIdsAsync(userId.Value, [page.Id], cancellationToken)).Contains(page.Id);

        return Result.Success(
            await PageDetailsMapping.ToDtoAsync(page, PageHierarchy.PathOf(page, ancestors), isFavorite, users, cancellationToken)
        );
    }
}
