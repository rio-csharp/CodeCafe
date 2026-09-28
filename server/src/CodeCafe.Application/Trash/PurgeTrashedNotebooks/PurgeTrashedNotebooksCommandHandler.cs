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

        while (await notebooks.ListTrashedAsync(userId.Value, skip: 0, BatchSize, cancellationToken) is { Count: > 0 } batch)
        {
            foreach (var notebook in batch)
            {
                notebooks.Remove(notebook);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
