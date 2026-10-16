using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.Shared;
using CodeCafe.Application.Pages;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Trash.RestorePageFromTrash;

public sealed class RestorePageFromTrashCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IUnitOfWork unitOfWork
) : ICommandHandler<RestorePageFromTrashCommand, Result>
{
    public async Task<Result> Handle(RestorePageFromTrashCommand command, CancellationToken cancellationToken)
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

        var notebook = context.Value!.Notebook;

        var trashed = await pages.ListTrashedByNotebookAsync(notebook.Id, cancellationToken);
        var childrenOf = trashed.ToLookup(candidate => candidate.ParentId);

        // Parent before descendants, so slug assignment walks the subtree in a stable order.
        var subtree = new List<Page>();
        var pending = new Queue<Page>([page]);
        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            subtree.Add(current);
            foreach (var child in childrenOf[current.Id])
            {
                pending.Enqueue(child);
            }
        }

        var live = await pages.ListByNotebookAsync(notebook.Id, cancellationToken);

        // A trashed or gone parent cannot take the subtree back, so it re-roots at the notebook.
        var parent = page.ParentId is { } parentId
            ? live.FirstOrDefault(candidate => candidate.Id == parentId)
            : null;

        // Every re-key starts from the slug the page carried into the trash, so a save retry
        // can never chain suffixes ("page-2-2").
        var originalSlugs = subtree.ToDictionary(node => node.Id, node => node.Slug);

        // Trashed pages hold no slugs (the unique index is partial), so every node of the
        // subtree must win its slug back from the live set before it goes live again.
        async Task AssignSlugsAsync(IReadOnlyList<Page> livePages, CancellationToken ct)
        {
            var assigned = livePages.Select(candidate => candidate.Slug).ToHashSet(StringComparer.Ordinal);
            foreach (var node in subtree)
            {
                var slug = originalSlugs[node.Id];
                if (assigned.Contains(slug))
                {
                    var candidates = await SlugAvailability.FindAvailableAsync(
                        slug,
                        Page.MaxSlugLength,
                        1,
                        (candidate, _) => Task.FromResult(assigned.Contains(candidate)),
                        ct
                    );
                    if (candidates.Count > 0)
                    {
                        slug = candidates[0];
                    }
                }

                if (!string.Equals(node.Slug, slug, StringComparison.Ordinal))
                {
                    node.ChangeSlug(slug);
                }

                assigned.Add(slug);
            }
        }

        await AssignSlugsAsync(live, cancellationToken);

        // The subtree root appends at the end of its (possibly new) parent's chain; the
        // original position is not restored. MoveTo is required: Unlink at delete time left
        // stale ParentId/SortKey/NextSiblingId on the page.
        var siblings = live
            .Where(candidate => candidate.ParentId == parent?.Id)
            .OrderBy(candidate => candidate.SortKey, StringComparer.Ordinal)
            .ToList();
        var after = siblings.Count > 0 ? siblings[^1] : null;
        page.MoveTo(parent?.Id, SiblingSortKeys.KeyForInsert(siblings, siblings.Count), null);
        PageChain.Link(page, notebook, parent, after);

        foreach (var node in subtree)
        {
            node.Restore();
        }

        // A writer can take a slug between the check and the save; re-running the assignment
        // from the original slugs yields fresh candidates, and the suffix space is unbounded.
        var save = await SlugConflict.SaveAsync(
            unitOfWork,
            async ct =>
            {
                var freshLive = await pages.ListByNotebookAsync(notebook.Id, ct);
                await AssignSlugsAsync(freshLive, ct);
                return true;
            },
            PageErrors.SlugAlreadyTaken,
            cancellationToken
        );
        if (save.Error is { } conflict)
        {
            return Result.Failure(conflict);
        }

        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
