using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;

namespace CodeCafe.Application.Trash.PurgeNotebook;

public sealed class PurgeNotebookCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IUnitOfWork unitOfWork
) : ICommandHandler<PurgeNotebookCommand, Result>
{
    public async Task<Result> Handle(PurgeNotebookCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await notebooks.LockLifecycleAsync(command.NotebookId, cancellationToken);

        var context = await NotebookAccess.RequireTrashedOwnerAsync(command.NotebookId, currentUserAccessor, notebooks, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure(error);
        }

        var notebook = context.Value!.Notebook;

        // Shares, favorites and pages cascade with the notebook row.
        notebooks.Remove(notebook);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
