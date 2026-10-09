using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Trash.PurgePage;

public sealed class PurgePageCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IUnitOfWork unitOfWork
) : ICommandHandler<PurgePageCommand, Result>
{
    public async Task<Result> Handle(PurgePageCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await pages.LockNotebookForPageStructureAsync(command.PageId, includeTrashed: true, cancellationToken);

        var page = await pages.FindTrashedByIdAsync(command.PageId, cancellationToken);
        if (page is null)
        {
            return Result.Failure(PageErrors.NotFound);
        }

        var context = await NotebookAccess.RequireOwnerAsync(page.NotebookId, currentUserAccessor, notebooks, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure(error);
        }

        var trashed = await pages.ListTrashedByNotebookAsync(context.Value!.Notebook.Id, cancellationToken);
        var childrenOf = trashed.ToLookup(candidate => candidate.ParentId);

        // No chain surgery: the subtree root was already unlinked from the live chain at
        // delete time, and its internal pointers die with the rows.
        var pending = new Queue<Page>([page]);
        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            // Favorites and shares cascade with the page row.
            pages.Remove(current);
            foreach (var child in childrenOf[current.Id])
            {
                pending.Enqueue(child);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
