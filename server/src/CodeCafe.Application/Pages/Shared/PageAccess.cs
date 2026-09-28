using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Domain.Notebooks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Sharing;

namespace CodeCafe.Application.Pages.Shared;

// Everything an access-checked handler needs, loaded in one go.
public sealed record PageAccessContext(Notebook Notebook, Page Page, IReadOnlyList<Page> Ancestors, Guid UserId);

// The gate every page read must pass: notebook access wins first, then a page share on the page
// or any ancestor grants the shared subtree. Write access follows the collaborator roles.
public static class PageAccess
{
    // Loads the page, its notebook and ancestors, and requires write access. Every failure
    // surfaces as page_not_found so the page's existence stays hidden.
    public static async Task<Result<PageAccessContext>> RequireWriteAsync(
        Guid pageId,
        ICurrentUserAccessor currentUserAccessor,
        INotebookRepository notebooks,
        IPageRepository pages,
        CancellationToken cancellationToken
    )
    {
        var context = await LoadAsync(pageId, loadAncestors: true, currentUserAccessor, notebooks, pages, cancellationToken);
        if (context is null || !CanWrite(context.Notebook, context.Page, context.Ancestors, context.UserId))
        {
            return Result.Failure<PageAccessContext>(PageErrors.NotFound);
        }

        return Result.Success(context);
    }

    // Share management belongs to the notebook owner, mirroring notebook shares.
    public static async Task<Result<PageAccessContext>> RequireOwnerAsync(
        Guid pageId,
        ICurrentUserAccessor currentUserAccessor,
        INotebookRepository notebooks,
        IPageRepository pages,
        CancellationToken cancellationToken
    )
    {
        var context = await LoadAsync(pageId, loadAncestors: false, currentUserAccessor, notebooks, pages, cancellationToken);
        if (context is null || context.Notebook.OwnerId != context.UserId)
        {
            return Result.Failure<PageAccessContext>(PageErrors.NotFound);
        }

        return Result.Success(context);
    }

    // Requires read access for the signed-in caller, passing the real error through so
    // access-code prompts still work.
    public static async Task<Result<PageAccessContext>> RequireReadAsync(
        Guid pageId,
        string? accessCode,
        ICurrentUserAccessor currentUserAccessor,
        INotebookRepository notebooks,
        IPageRepository pages,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken
    )
    {
        var context = await LoadAsync(pageId, loadAncestors: true, currentUserAccessor, notebooks, pages, cancellationToken);
        if (context is null)
        {
            return Result.Failure<PageAccessContext>(PageErrors.NotFound);
        }

        var error = CheckRead(context.Notebook, context.Page, context.Ancestors, context.UserId, accessCode, passwordHasher);
        return error is not null ? Result.Failure<PageAccessContext>(error) : Result.Success(context);
    }

    private static async Task<PageAccessContext?> LoadAsync(
        Guid pageId,
        bool loadAncestors,
        ICurrentUserAccessor currentUserAccessor,
        INotebookRepository notebooks,
        IPageRepository pages,
        CancellationToken cancellationToken
    )
    {
        if (currentUserAccessor.User?.Id is not { } userId)
        {
            return null;
        }

        var page = await pages.FindByIdAsync(pageId, cancellationToken);
        var notebook = page is not null ? await notebooks.FindByIdAsync(page.NotebookId, cancellationToken) : null;
        if (page is null || notebook is null)
        {
            return null;
        }

        var ancestors = loadAncestors
            ? await PageHierarchy.LoadAncestorsAsync(page, pages, cancellationToken)
            : [];
        return new PageAccessContext(notebook, page, ancestors, userId);
    }

    public static Error? CheckRead(
        Notebook notebook,
        Page page,
        IReadOnlyList<Page> ancestors,
        Guid? userId,
        string? accessCode,
        IPasswordHasher passwordHasher
    )
    {
        var notebookError = NotebookAccess.CheckRead(notebook, userId, accessCode, passwordHasher);
        if (notebookError is null)
        {
            return null;
        }

        if (userId is not null
            && (page.IsSharedWith(userId.Value) || ancestors.Any(ancestor => ancestor.IsSharedWith(userId.Value))))
        {
            return null;
        }

        // The page is what was asked for, so a notebook-level denial surfaces as
        // page_not_found; only AccessCodeRequired passes through, so the client knows to prompt.
        return notebookError == NotebookErrors.NotFound ? PageErrors.NotFound : notebookError;
    }

    // Page shares with the Editor role extend write access into the shared subtree.
    private static bool CanWrite(Notebook notebook, Page? page, IReadOnlyList<Page> ancestors, Guid userId)
        => notebook.OwnerId == userId
            || notebook.SharedRoleFor(userId) == CollaboratorRole.Editor
            || page?.SharedRoleFor(userId) == CollaboratorRole.Editor
            || ancestors.Any(ancestor => ancestor.SharedRoleFor(userId) == CollaboratorRole.Editor);
}
