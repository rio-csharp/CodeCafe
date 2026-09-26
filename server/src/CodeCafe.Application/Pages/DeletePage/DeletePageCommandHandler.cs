using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Pages.DeletePage;

public sealed class DeletePageCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IUnitOfWork unitOfWork
) : ICommandHandler<DeletePageCommand, Result>
{
    public async Task<Result> Handle(DeletePageCommand command, CancellationToken cancellationToken)
    {
        var context = await PageAccess.RequireWriteAsync(command.PageId, currentUserAccessor, notebooks, pages, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure(error);
        }

        var (notebook, page, ancestors, _) = context.Value!;

        var all = await pages.ListByNotebookAsync(notebook.Id, cancellationToken);
        var childrenOf = all.ToLookup(candidate => candidate.ParentId);

        // Unlink the subtree root so the live chain skips it. The subtree keeps its internal
        // pointers so a future restore can re-link the whole chain in one step.
        PageChain.Unlink(page, notebook, ancestors, all);

        var deletedAtUtc = DateTimeOffset.UtcNow;
        var pending = new Queue<Page>([page]);
        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            current.SoftDelete(deletedAtUtc);
            foreach (var child in childrenOf[current.Id])
            {
                pending.Enqueue(child);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
