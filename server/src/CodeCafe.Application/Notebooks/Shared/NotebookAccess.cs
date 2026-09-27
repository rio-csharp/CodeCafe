using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Notebooks.Shared;

// Everything an access-checked notebook handler needs, mirroring PageAccessContext.
public sealed record NotebookAccessContext(Notebook Notebook, Guid UserId);

// The gate every notebook write must pass. Every failure surfaces as notebook_not_found so the
// notebook's existence stays hidden, mirroring PageAccess.
public static class NotebookAccess
{
    public static async Task<Result<NotebookAccessContext>> RequireOwnerAsync(
        string notebookIdOrSlug,
        ICurrentUserAccessor currentUserAccessor,
        INotebookRepository notebooks,
        CancellationToken cancellationToken
    )
    {
        if (currentUserAccessor.User?.Id is not { } userId)
        {
            return Result.Failure<NotebookAccessContext>(NotebookErrors.NotFound);
        }

        var notebook = await notebooks.FindByIdOrSlugAsync(notebookIdOrSlug, cancellationToken);
        return notebook is null || notebook.OwnerId != userId
            ? Result.Failure<NotebookAccessContext>(NotebookErrors.NotFound)
            : Result.Success(new NotebookAccessContext(notebook, userId));
    }

    // Trash operations address a notebook by id and must see past the soft-delete filter.
    public static async Task<Result<NotebookAccessContext>> RequireTrashedOwnerAsync(
        Guid notebookId,
        ICurrentUserAccessor currentUserAccessor,
        INotebookRepository notebooks,
        CancellationToken cancellationToken
    )
    {
        if (currentUserAccessor.User?.Id is not { } userId)
        {
            return Result.Failure<NotebookAccessContext>(NotebookErrors.NotFound);
        }

        var notebook = await notebooks.FindTrashedByIdAsync(notebookId, cancellationToken);
        return notebook is null || notebook.OwnerId != userId
            ? Result.Failure<NotebookAccessContext>(NotebookErrors.NotFound)
            : Result.Success(new NotebookAccessContext(notebook, userId));
    }

    // Favorites are per-user, so anyone the notebook is listed for may set their own.
    public static async Task<Result<NotebookAccessContext>> RequireOwnerOrSharedAsync(
        string notebookIdOrSlug,
        ICurrentUserAccessor currentUserAccessor,
        INotebookRepository notebooks,
        CancellationToken cancellationToken
    )
    {
        if (currentUserAccessor.User?.Id is not { } userId)
        {
            return Result.Failure<NotebookAccessContext>(NotebookErrors.NotFound);
        }

        var notebook = await notebooks.FindByIdOrSlugAsync(notebookIdOrSlug, cancellationToken);
        return notebook is null || (notebook.OwnerId != userId && !notebook.IsSharedWith(userId))
            ? Result.Failure<NotebookAccessContext>(NotebookErrors.NotFound)
            : Result.Success(new NotebookAccessContext(notebook, userId));
    }
}
