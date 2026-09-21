using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;

namespace CodeCafe.Application.Notebooks.SetNotebookFavorite;

public sealed class SetNotebookFavoriteCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IUnitOfWork unitOfWork
) : ICommandHandler<SetNotebookFavoriteCommand, Result>
{
    public async Task<Result> Handle(SetNotebookFavoriteCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        var notebook = userId is not null
            ? await notebooks.FindByIdOrSlugAsync(command.NotebookIdOrSlug, cancellationToken)
            : null;

        // Favorites are per-user, so anyone the notebook is listed for can set their own.
        if (notebook is null || (notebook.OwnerId != userId && !notebook.IsSharedWith(userId!.Value)))
        {
            return Result.Failure(NotebookErrors.NotFound);
        }

        await notebooks.SetFavoriteAsync(notebook.Id, userId.Value, command.IsFavorite, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
