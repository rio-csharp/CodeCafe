using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.ChangePageSlug;

public sealed class ChangePageSlugCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IUserRepository users,
    IUnitOfWork unitOfWork
) : ICommandHandler<ChangePageSlugCommand, Result<PageDetailsDto>>
{
    public async Task<Result<PageDetailsDto>> Handle(ChangePageSlugCommand command, CancellationToken cancellationToken)
    {
        var context = await PageAccess.RequireWriteAsync(command.PageId, currentUserAccessor, notebooks, pages, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure<PageDetailsDto>(error);
        }

        var (notebook, page, ancestors, userId) = context.Value!;

        var picked = string.IsNullOrWhiteSpace(command.Slug) ? null : PageSlug.Normalize(command.Slug);
        var target = picked ?? PageSlug.GenerateFromTitle(page.Title);

        // Already there: regenerating would only suffix a page for holding its own slug.
        if (!string.Equals(page.Slug, target, StringComparison.Ordinal))
        {
            if (picked is not null && await pages.FindBySlugAsync(notebook.Id, picked, cancellationToken) is not null)
            {
                return Result.Failure<PageDetailsDto>(PageErrors.SlugAlreadyTaken);
            }

            var slug = picked;
            // A picked slug is the caller's to fix on a lost race; a derived one is ours, so the
            // conflict helper may move the page to a fresh candidate instead.
            Func<CancellationToken, Task<bool>>? reKey = null;
            if (slug is null)
            {
                slug = await PageSlugPolicy.FindAvailableAsync(notebook.Id, page.Title, pages, cancellationToken);
                if (slug is null)
                {
                    return Result.Failure<PageDetailsDto>(PageErrors.SlugAlreadyTaken);
                }

                reKey = ct => PageSlugPolicy.ReKeyAsync(page, notebook.Id, page.Title, pages, ct);
            }

            page.ChangeSlug(slug);

            var save = await SlugConflict.SaveAsync(unitOfWork, reKey, PageErrors.SlugAlreadyTaken, cancellationToken);
            if (save.Error is { } conflict)
            {
                return Result.Failure<PageDetailsDto>(conflict);
            }
        }

        var isFavorite = (await pages.FindFavoriteIdsAsync(userId, [page.Id], cancellationToken)).Contains(page.Id);

        return Result.Success(
            await PageDetailsMapping.ToDtoAsync(
                page,
                // Derived, so descendants of a renamed page move with it: the path is rebuilt
                // from the ancestor chain on every read, never stored.
                PageHierarchy.PathOf(page, ancestors),
                isFavorite,
                includeShares: notebook.OwnerId == userId,
                // The handler just passed RequireWriteAsync for this page.
                canWrite: true,
                users,
                cancellationToken
            )
        );
    }
}
