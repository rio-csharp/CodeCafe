using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.SharePage;

public sealed class SharePageCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository users,
    INotebookRepository notebooks,
    IPageRepository pages,
    IUnitOfWork unitOfWork
) : ICommandHandler<SharePageCommand, Result>
{
    public async Task<Result> Handle(SharePageCommand command, CancellationToken cancellationToken)
    {
        var context = await PageAccess.RequireOwnerAsync(command.PageId, currentUserAccessor, notebooks, pages, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure(error);
        }

        var (notebook, page, _, _) = context.Value!;

        var target = await users.FindByEmailAsync(AuthInput.NormalizeEmail(command.Email), cancellationToken);
        if (target is null)
        {
            return Result.Failure(PageErrors.ShareTargetNotFound);
        }

        if (target.Id == notebook.OwnerId)
        {
            return Result.Failure(PageErrors.CannotShareWithOwner);
        }

        page.Share(target.Id, command.Role);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
