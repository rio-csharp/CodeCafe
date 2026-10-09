using CodeCafe.Application.Auth;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;

namespace CodeCafe.Application.Trash.PurgeTrashedNotebooks;

public sealed class PurgeTrashedNotebooksCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IUnitOfWork unitOfWork
) : ICommandHandler<PurgeTrashedNotebooksCommand, Result>
{
    // Emptied in batches so a huge trash does not load every row at once.
    private const int BatchSize = 100;

    public async Task<Result> Handle(PurgeTrashedNotebooksCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        if (userId is null)
        {
            return Result.Failure(AuthErrors.UserNotFound);
        }

        while (true)
        {
            await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
            var ids = (await notebooks.ListTrashedAsync(userId.Value, skip: 0, BatchSize, cancellationToken))
                .Select(notebook => notebook.Id)
                .Order()
                .ToList();
            if (ids.Count == 0)
            {
                return Result.Success();
            }

            foreach (var notebookId in ids)
            {
                await notebooks.LockLifecycleAsync(notebookId, cancellationToken);
                var notebook = await notebooks.FindTrashedByIdAsync(notebookId, cancellationToken);
                if (notebook is not null && notebook.OwnerId == userId.Value)
                {
                    notebooks.Remove(notebook);
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }
}
