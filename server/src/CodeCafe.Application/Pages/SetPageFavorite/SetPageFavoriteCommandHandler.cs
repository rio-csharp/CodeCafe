using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.SetPageFavorite;

public sealed class SetPageFavoriteCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork
) : ICommandHandler<SetPageFavoriteCommand, Result>
{
    public async Task<Result> Handle(SetPageFavoriteCommand command, CancellationToken cancellationToken)
    {
        // Favorites are per-user, so anyone who can read the page can set their own.
        var context = await PageAccess.RequireReadAsync(
            command.PageId,
            accessCode: null,
            currentUserAccessor,
            notebooks,
            pages,
            passwordHasher,
            cancellationToken
        );
        if (context.Error is { } error)
        {
            return Result.Failure(error);
        }

        await pages.SetFavoriteAsync(context.Value!.Page.Id, context.Value!.UserId, command.IsFavorite, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
