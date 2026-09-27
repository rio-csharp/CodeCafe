using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;

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
        var context = await NotebookAccess.RequireOwnerAsync(command.NotebookIdOrSlug, currentUserAccessor, notebooks, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure(error);
        }

        var notebook = context.Value!.Notebook;

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
