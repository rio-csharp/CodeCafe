using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.RevokePageShare;

public sealed class RevokePageShareCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IUnitOfWork unitOfWork
) : ICommandHandler<RevokePageShareCommand, Result>
{
    public async Task<Result> Handle(RevokePageShareCommand command, CancellationToken cancellationToken)
    {
        var context = await PageAccess.RequireOwnerAsync(command.PageId, currentUserAccessor, notebooks, pages, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure(error);
        }

        context.Value!.Page.RevokeShare(command.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
