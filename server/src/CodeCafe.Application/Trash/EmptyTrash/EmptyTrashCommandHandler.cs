using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;

namespace CodeCafe.Application.Trash.EmptyTrash;

public sealed class EmptyTrashCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IUnitOfWork unitOfWork
) : ICommandHandler<EmptyTrashCommand, Result>
{
    // Emptied in batches so a huge trash does not load every row at once.
    private const int BatchSize = 100;

    public async Task<Result> Handle(EmptyTrashCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        if (userId is null)
        {
            return Result.Success();
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
