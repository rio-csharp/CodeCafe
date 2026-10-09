using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Trash.RestoreNotebookFromTrash;

public sealed class RestoreNotebookFromTrashCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IUnitOfWork unitOfWork
) : ICommandHandler<RestoreNotebookFromTrashCommand, Result>
{
    public async Task<Result> Handle(RestoreNotebookFromTrashCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await notebooks.LockLifecycleAsync(command.NotebookId, cancellationToken);

        var context = await NotebookAccess.RequireTrashedOwnerAsync(command.NotebookId, currentUserAccessor, notebooks, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure(error);
        }

        var notebook = context.Value!.Notebook;
        var trashedSlug = notebook.Slug;

        // A trashed notebook no longer holds its slug, so it may have been taken in the meantime.
        // Re-keying and un-deleting share one save, so the row is never briefly live on a taken slug.
        if (!await TryReKeyAsync(notebook, trashedSlug, cancellationToken))
        {
            return Result.Failure(NotebookErrors.SlugAlreadyTaken);
        }

        notebook.Restore();

        // A writer can take the slug between the check and the save; the notebook never picked it,
        // so re-key and retry rather than fail the restore.
        var save = await SlugConflict.SaveAsync(
            unitOfWork,
            ct => TryReKeyAsync(notebook, trashedSlug, ct),
            NotebookErrors.SlugAlreadyTaken,
            cancellationToken
        );
        if (!save.IsSuccess)
        {
            return save;
        }

        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    // Keeps the notebook on its own slug when it is still free, otherwise moves it to the best
    // suffixed variant. False means no free candidate is left.
    private async Task<bool> TryReKeyAsync(Notebook notebook, string trashedSlug, CancellationToken cancellationToken)
    {
        var candidates = await SlugAvailability.FindAvailableAsync(
            trashedSlug,
            Notebook.MaxSlugLength,
            1,
            async (candidate, ct) => await notebooks.FindBySlugAsync(candidate, ct) is not null,
            cancellationToken
        );

        if (candidates.Count == 0)
        {
            return false;
        }

        if (!string.Equals(notebook.Slug, candidates[0], StringComparison.Ordinal))
        {
            notebook.ChangeSlug(candidates[0]);
        }

        return true;
    }
}
