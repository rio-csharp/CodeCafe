using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Pages.Shared;

// The tail every page-details read shares: authorize, then project to PageDetailsDto with the
// caller's favorite flag. GetPage and GetPageByPath both end here; they differ only in how the
// page is located. RequireReadAsync is not an option for them because it rejects anonymous
// callers, while these reads must stay reachable for the access-code flow.
public static class PageReadProjection
{
    public static async Task<Result<PageDetailsDto>> ToDetailsAsync(
        Notebook notebook,
        Page page,
        Guid? userId,
        string? accessCode,
        IPageRepository pages,
        IUserRepository users,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken
    )
    {
        var ancestors = await PageHierarchy.LoadAncestorsAsync(page, pages, cancellationToken);
        var error = PageAccess.CheckRead(notebook, page, ancestors, userId, accessCode, passwordHasher);
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
