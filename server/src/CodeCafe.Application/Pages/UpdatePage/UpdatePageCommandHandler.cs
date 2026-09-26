using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.UpdatePage;

public sealed class UpdatePageCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IUserRepository users,
    IUnitOfWork unitOfWork
) : ICommandHandler<UpdatePageCommand, Result<PageDetailsDto>>
{
    public async Task<Result<PageDetailsDto>> Handle(UpdatePageCommand command, CancellationToken cancellationToken)
    {
        var context = await PageAccess.RequireWriteAsync(command.PageId, currentUserAccessor, notebooks, pages, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure<PageDetailsDto>(error);
        }

        var (_, page, ancestors, userId) = context.Value!;

        // Null fields keep their current values.
        if (command.Title?.Trim() is { Length: > 0 } title)
        {
            page.Rename(title);
        }

        if (command.IsArchived is { } isArchived)
        {
            page.SetArchived(isArchived);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var isFavorite = (await pages.FindFavoriteIdsAsync(userId, [page.Id], cancellationToken)).Contains(page.Id);

        return Result.Success(
            await PageDetailsMapping.ToDtoAsync(page, PageHierarchy.PathOf(page, ancestors), isFavorite, users, cancellationToken)
        );
    }
}
