using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;

namespace CodeCafe.Application.Notebooks.SetNotebookFavorite;

public sealed class SetNotebookFavoriteCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IUnitOfWork unitOfWork
) : ICommandHandler<SetNotebookFavoriteCommand, Result>
{
    public async Task<Result> Handle(SetNotebookFavoriteCommand command, CancellationToken cancellationToken)
    {
        var context = await NotebookAccess.RequireOwnerOrSharedAsync(command.NotebookIdOrSlug, currentUserAccessor, notebooks, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure(error);
        }

        var notebook = context.Value!.Notebook;
        var userId = context.Value!.UserId;

        await notebooks.SetFavoriteAsync(notebook.Id, userId, command.IsFavorite, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
