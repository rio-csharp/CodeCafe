using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Blocks.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Application.Revisions.Abstractions;
using CodeCafe.Application.Revisions.Shared;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Pages;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Revisions.RestorePageToRevision;

// Rolls the whole page back to its state at AtUtc. The log is complete (every insert, update,
// move and delete writes a row carrying the post-change parent, sort key and payload), so the
// state at any instant is exactly "each block's latest row at or before that instant". The diff
// against the live tree is then applied with ORIGINAL block ids preserved: blocks that should
// not exist are deleted, missing ones are re-created, and the survivors' payloads and structure
// are put back — and the restore itself is recorded as one more batch, so history stays
// append-only and the restore can be undone by restoring again.
public sealed class RestorePageToRevisionCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IBlockRepository blocks,
    IBlockRevisionRepository revisions,
    IChangeSourceAccessor changeSource,
    IUnitOfWork unitOfWork
) : ICommandHandler<RestorePageToRevisionCommand, Result>
{
    public async Task<Result> Handle(RestorePageToRevisionCommand command, CancellationToken cancellationToken)
    {
        var context = await PageAccess.RequireWriteAsync(command.PageId, currentUserAccessor, notebooks, pages, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure(error);
        }

        var (_, page, _, _) = context.Value!;

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await blocks.LockPageAsync(command.PageId, cancellationToken);
        var current = (await blocks.ListByPageAsync(command.PageId, cancellationToken)).ToList();
        // Reads the page's ENTIRE history to reconstruct the target state — the spot the
        // retention/compaction strategies on BlockRevision would shrink for long-lived pages.
        var history = await revisions.ListAllByPageAsync(command.PageId, cancellationToken);

        // Latest row per block at or before the target instant; a block whose latest row is a
        // deletion (or that has no row at all) does not exist in the desired state.
        var desired = history
            .Where(row => row.CreatedAtUtc <= command.AtUtc)
            .GroupBy(row => row.BlockId)
            .Select(group => group.OrderByDescending(row => row.CreatedAtUtc).ThenByDescending(row => row.Id).First())
            .Where(row => row.ChangeKind != BlockChangeKind.Deleted)
            .ToDictionary(row => row.BlockId);

        var batchId = Guid.CreateVersion7();
        var source = changeSource.Source;

        // Blocks that must not exist: record and remove them BEFORE the rebuild, so the tree
        // passed to RebuildStructure is exactly the desired set. Their current ParentBlockId /
        // SortKey are still intact for the snapshot rows.
        var removed = current.Where(block => !desired.ContainsKey(block.Id)).ToList();
        if (removed.Count > 0)
        {
            revisions.AddRange(RevisionRecording.Deleted(removed, batchId, source));
            blocks.RemoveRange(removed);
        }

        var survivors = current.Where(block => desired.ContainsKey(block.Id)).ToList();

        // Blocks that existed at the target instant but not now: re-create with original ids.
        var recreated = new List<Block>();
        var survivorIds = survivors.Select(block => block.Id).ToHashSet();
        foreach (var row in desired.Values)
        {
            if (survivorIds.Contains(row.BlockId))
            {
                continue;
            }

            var block = Block.Restore(
                row.BlockId,
                command.PageId,
                row.ParentBlockId,
                row.Type,
                row.ContentJson,
                row.PlainText,
                row.SortKey
            );
            recreated.Add(block);
            blocks.Add(block);
        }

        // Payloads of the survivors; moves are detected against the historical placement and
        // recorded after the rebuild (the recording reads the block's post-move pointers).
        var moved = new List<Block>();
        foreach (var block in survivors)
        {
            var row = desired[block.Id];
            if (!string.Equals(block.ContentJson, row.ContentJson, StringComparison.Ordinal))
            {
                block.UpdateContent(row.ContentJson, row.PlainText);
                revisions.Add(RevisionRecording.Updated(block, batchId, source));
            }

            if (block.ParentBlockId != row.ParentBlockId
                || !string.Equals(block.SortKey, row.SortKey, StringComparison.Ordinal))
            {
                moved.Add(block);
            }
        }

        var restored = survivors.Concat(recreated).ToList();
        var placements = desired.ToDictionary(
            pair => pair.Key,
            pair => new BlockPlacement(pair.Value.ParentBlockId, pair.Value.SortKey)
        );
        BlockChain.RebuildStructure(page, restored, placements);

        foreach (var block in recreated)
        {
            revisions.Add(RevisionRecording.Added(block, batchId, source));
        }

        foreach (var block in moved)
        {
            revisions.Add(RevisionRecording.Moved(block, batchId, source));
        }

        page.Touch();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
