using System.Text.Json;

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

namespace CodeCafe.Application.Revisions.RestoreBlockRevision;

// Restores ONE block to one historical revision.
// - Block alive: put the snapshot payload back (structure untouched — even when the target row
//   is a Deleted one from an earlier era; resurrecting its snapshot children would duplicate
//   the ones still alive).
// - Block deleted: re-create it with its ORIGINAL id, and when the target is the Deleted row
//   carrying the subtree snapshot, bring the whole subtree back with it.
public sealed class RestoreBlockRevisionCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IBlockRepository blocks,
    IBlockRevisionRepository revisions,
    IChangeSourceAccessor changeSource,
    IUnitOfWork unitOfWork
) : ICommandHandler<RestoreBlockRevisionCommand, Result>
{
    public async Task<Result> Handle(RestoreBlockRevisionCommand command, CancellationToken cancellationToken)
    {
        var context = await PageAccess.RequireWriteAsync(command.PageId, currentUserAccessor, notebooks, pages, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure(error);
        }

        var (_, page, _, _) = context.Value!;

        // Restoring rewrites structure (re-created blocks splice into chains), so it plays by
        // the same rules as the other structural mutations: lock first, then read the chain.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await blocks.LockPageAsync(command.PageId, cancellationToken);
        var working = (await blocks.ListByPageAsync(command.PageId, cancellationToken)).ToList();

        // The newest row carrying the requested block version wins: re-created blocks restart
        // their counter at 1, so a number can appear in several eras.
        var history = await revisions.ListAllByBlockAsync(command.PageId, command.BlockId, cancellationToken);
        var target = history
            .Where(row => row.BlockVersion == command.BlockVersion)
            .OrderByDescending(row => row.CreatedAtUtc)
            .ThenByDescending(row => row.Id)
            .FirstOrDefault();
        if (target is null)
        {
            return Result.Failure(RevisionErrors.NotFound);
        }

        var batchId = Guid.CreateVersion7();
        var source = changeSource.Source;
        var live = working.FirstOrDefault(block => block.Id == command.BlockId);
        if (live is not null)
        {
            live.UpdateContent(target.ContentJson, target.PlainText);
            revisions.Add(RevisionRecording.Updated(live, batchId, source));
        }
        else
        {
            var recreated = Recreate(command, page, target, working);
            if (recreated.Error is { } recreateError)
            {
                return Result.Failure(recreateError);
            }

            foreach (var block in recreated.Value!)
            {
                blocks.Add(block);
                revisions.Add(RevisionRecording.Added(block, batchId, source));
            }
        }

        page.Touch();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }

    private static Result<List<Block>> Recreate(
        RestoreBlockRevisionCommand command,
        Page page,
        BlockRevision target,
        List<Block> working
    )
    {
        // The parent recorded in the snapshot may itself be gone; fall back to the top level.
        var parent = target.ParentBlockId is { } parentId
            ? working.FirstOrDefault(block => block.Id == parentId)
            : null;
        var siblings = BlockChain.OrderByChain(page, working, parent?.Id).ToList();

        var created = new List<Block>();
        if (target.ChangeKind == BlockChangeKind.Deleted && target.SubtreeJson is not null)
        {
            using var snapshot = JsonDocument.Parse(target.SubtreeJson);
            RecreateNode(snapshot.RootElement, command.PageId, page, parent, siblings, created);
        }
        else
        {
            var block = Block.Restore(
                command.BlockId,
                command.PageId,
                parent?.Id,
                target.Type,
                target.ContentJson,
                target.PlainText,
                BlockSiblingSortKeys.KeyForInsert(siblings, siblings.Count)
            );
            BlockChain.Insert(block, page, parent, siblings.Count > 0 ? siblings[^1] : null, null);
            created.Add(block);
        }

        // A snapshot id colliding with a live block would corrupt the tree; refuse the restore.
        var workingIds = working.Select(block => block.Id).ToHashSet();
        return created.Any(block => workingIds.Contains(block.Id))
            ? Result.Failure<List<Block>>(RevisionErrors.RestoreConflict)
            : Result.Success(created);
    }

    private static void RecreateNode(
        JsonElement node,
        Guid pageId,
        Page page,
        Block? parent,
        List<Block> siblings,
        List<Block> created
    )
    {
        var block = Block.Restore(
            node.GetProperty("id").GetGuid(),
            pageId,
            parent?.Id,
            node.GetProperty("type").GetString()!,
            node.GetProperty("content").GetRawText(),
            node.GetProperty("plainText").GetString() ?? string.Empty,
            BlockSiblingSortKeys.KeyForInsert(siblings, siblings.Count)
        );
        BlockChain.Insert(block, page, parent, siblings.Count > 0 ? siblings[^1] : null, null);
        siblings.Add(block);
        created.Add(block);

        var children = new List<Block>();
        foreach (var child in node.GetProperty("children").EnumerateArray())
        {
            RecreateNode(child, pageId, page, block, children, created);
        }
    }
}
