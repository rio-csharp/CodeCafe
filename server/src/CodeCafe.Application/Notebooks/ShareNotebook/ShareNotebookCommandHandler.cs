using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;

namespace CodeCafe.Application.Notebooks.ShareNotebook;

public sealed class ShareNotebookCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository users,
    INotebookRepository notebooks,
    IUnitOfWork unitOfWork
) : ICommandHandler<ShareNotebookCommand, Result>
{
    public async Task<Result> Handle(ShareNotebookCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        var notebook = userId is not null
            ? await notebooks.FindByIdOrSlugAsync(command.NotebookIdOrSlug, cancellationToken)
            : null;
        if (notebook is null || notebook.OwnerId != userId)
        {
            return Result.Failure(NotebookErrors.NotFound);
        }

        var target = await users.FindByEmailAsync(AuthInput.NormalizeEmail(command.Email), cancellationToken);
        if (target is null)
        {
            return Result.Failure(NotebookErrors.ShareTargetNotFound);
        }

        if (target.Id == notebook.OwnerId)
        {
            return Result.Failure(NotebookErrors.CannotShareWithOwner);
        }

        notebook.Share(target.Id, command.Role);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
