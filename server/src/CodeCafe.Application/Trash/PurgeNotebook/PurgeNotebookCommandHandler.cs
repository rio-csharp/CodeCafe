using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.Abstractions;

namespace CodeCafe.Application.Trash.PurgeNotebook;

public sealed class PurgeNotebookCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IUnitOfWork unitOfWork
) : ICommandHandler<PurgeNotebookCommand, Result>
{
    public async Task<Result> Handle(PurgeNotebookCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        var notebook = userId is not null
            ? await notebooks.FindTrashedByIdAsync(command.NotebookId, cancellationToken)
            : null;
        if (notebook is null || notebook.OwnerId != userId)
        {
            return Result.Failure(NotebookErrors.NotFound);
        }

        // Shares, favorites and pages cascade with the notebook row.
        notebooks.Remove(notebook);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
