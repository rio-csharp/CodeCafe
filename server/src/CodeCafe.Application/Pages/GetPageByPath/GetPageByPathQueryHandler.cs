using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.GetPageByPath;

public sealed class GetPageByPathQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IUserRepository users,
    IPasswordHasher passwordHasher
) : IQueryHandler<GetPageByPathQuery, Result<PageDetailsDto>>
{
    public async Task<Result<PageDetailsDto>> Handle(GetPageByPathQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        var notebook = await notebooks.FindByIdOrSlugAsync(query.NotebookIdOrSlug, cancellationToken);
        if (notebook is null)
        {
            return Result.Failure<PageDetailsDto>(NotebookErrors.NotFound);
        }

        var page = await PageHierarchy.ResolveByPathAsync(notebook.Id, query.Path, pages, cancellationToken);
        if (page is null)
        {
            return Result.Failure<PageDetailsDto>(PageErrors.NotFound);
        }

        var ancestors = await PageHierarchy.LoadAncestorsAsync(page, pages, cancellationToken);
        var error = PageAccess.CheckRead(notebook, page, ancestors, userId, query.AccessCode, passwordHasher);
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
