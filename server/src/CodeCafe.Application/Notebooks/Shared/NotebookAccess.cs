using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Notebooks.Shared;

// Everything an access-checked notebook handler needs, mirroring PageAccessContext.
public sealed record NotebookAccessContext(Notebook Notebook, Guid UserId);

// The single gate for reaching a notebook, read or write, mirroring PageAccess: the Require*
// methods load and authorize, CheckRead judges one the caller already holds.
public static class NotebookAccess
{
    // Every write failure surfaces as notebook_not_found so the notebook's existence stays hidden.
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

        return CheckOwner(await notebooks.FindByIdOrSlugAsync(notebookIdOrSlug, cancellationToken), userId);
    }

    // Page trash operations know the notebook only by the page's NotebookId. A trashed notebook
    // does not pass: FindByIdAsync honours the soft-delete filter, so it surfaces as NotFound.
    public static async Task<Result<NotebookAccessContext>> RequireOwnerAsync(
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

        return CheckOwner(await notebooks.FindByIdAsync(notebookId, cancellationToken), userId);
    }

    private static Result<NotebookAccessContext> CheckOwner(Notebook? notebook, Guid userId)
        => notebook is null || notebook.OwnerId != userId
            ? Result.Failure<NotebookAccessContext>(NotebookErrors.NotFound)
            : Result.Success(new NotebookAccessContext(notebook, userId));

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

    // Page creation happens before any page exists, so its write check is notebook-level only.
    public static async Task<Result<NotebookAccessContext>> RequireWriterAsync(
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
        return notebook is null || (notebook.OwnerId != userId && notebook.SharedRoleFor(userId) != CollaboratorRole.Editor)
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

    // The gate every notebook read path must pass: details today, tree/export once they leave
    // their skeletons. Returns null when the read is allowed, else the error to return.
    public static Error? CheckRead(Notebook notebook, Guid? userId, string? accessCode, IPasswordHasher passwordHasher)
    {
        var isPrivilegedReader =
            userId is not null && (notebook.OwnerId == userId || notebook.IsSharedWith(userId.Value));

        return notebook.Visibility switch
        {
            // Strangers get NotFound so existence is not leaked.
            NotebookVisibility.Private => isPrivilegedReader ? null : NotebookErrors.NotFound,

            // The access code only gates Unlisted notebooks; owners and collaborators bypass it.
            NotebookVisibility.Unlisted when notebook.AccessCodeHash is not null && !isPrivilegedReader =>
                accessCode is not null && passwordHasher.Verify(accessCode, notebook.AccessCodeHash)
                    ? null
                    : NotebookErrors.AccessCodeRequired,

            _ => null,
        };
    }
}
