using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Pages.MovePage;

public sealed class MovePageCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IUserRepository users,
    IUnitOfWork unitOfWork
) : ICommandHandler<MovePageCommand, Result<PageDetailsDto>>
{
    public async Task<Result<PageDetailsDto>> Handle(MovePageCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await pages.LockNotebookForPageStructureAsync(command.PageId, includeTrashed: false, cancellationToken);

        var context = await PageAccess.RequireWriteAsync(command.PageId, currentUserAccessor, notebooks, pages, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure<PageDetailsDto>(error);
        }

        var (notebook, page, ancestors, userId) = context.Value!;

        // A null parent path targets the notebook root.
        var newParent = command.NewParentPath is not null
            ? await PageHierarchy.ResolveByPathAsync(notebook.Id, command.NewParentPath, pages, cancellationToken)
            : null;
        if (command.NewParentPath is not null && newParent is null)
        {
            return Result.Failure<PageDetailsDto>(PageErrors.ParentNotFound);
        }

        var newParentAncestors = newParent is null
            ? []
            : await PageHierarchy.LoadAncestorsAsync(newParent, pages, cancellationToken);
        if (newParent is not null && PageChain.IsSelfOrDescendant(page, newParent, newParentAncestors))
        {
            return Result.Failure<PageDetailsDto>(PageErrors.CannotMoveIntoDescendant);
        }

        var newSiblings = (await pages.ListChildrenAsync(notebook.Id, newParent?.Id, cancellationToken))
            .Where(sibling => sibling.Id != page.Id)
            .ToList();

        // No AfterPageId appends after the last sibling, matching how tree UIs grow.
        var after = command.AfterPageId is { } afterPageId
            ? newSiblings.FirstOrDefault(sibling => sibling.Id == afterPageId)
            : newSiblings.Count > 0 ? newSiblings[^1] : null;
        if (command.AfterPageId is not null && after is null)
        {
            return Result.Failure<PageDetailsDto>(PageErrors.AfterPageNotFound);
        }

        var insertIndex = after is null ? 0 : newSiblings.IndexOf(after) + 1;
        var oldSiblings = await pages.ListChildrenAsync(notebook.Id, page.ParentId, cancellationToken);
        PageChain.Move(page, notebook, newParent, ancestors, oldSiblings, newSiblings, insertIndex);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var newAncestors = newParent is null
            ? (IReadOnlyList<Page>)[]
            : newParentAncestors.Append(newParent).ToList();
        var isFavorite = (await pages.FindFavoriteIdsAsync(userId, [page.Id], cancellationToken)).Contains(page.Id);

        return Result.Success(
            await PageDetailsMapping.ToDtoAsync(
                page,
                PageHierarchy.PathOf(page, newAncestors),
                isFavorite,
                includeShares: notebook.OwnerId == userId,
                // The handler just passed RequireWriteAsync for this page.
                canWrite: true,
                users,
                cancellationToken
            )
        );
    }
}
