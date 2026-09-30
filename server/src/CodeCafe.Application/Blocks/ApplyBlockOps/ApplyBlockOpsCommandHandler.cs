using System.Text.Json;

using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Blocks.Abstractions;
using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Pages.Abstractions;
using CodeCafe.Application.Pages.Shared;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Blocks.ApplyBlockOps;

// Applies a batch of block ops in one all-or-nothing transaction. Ops run strictly in order and
// each one sees the working state produced by the earlier ops — including TempId mappings and
// revisions bumped earlier in the same batch.
public sealed class ApplyBlockOpsCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    INotebookRepository notebooks,
    IPageRepository pages,
    IBlockRepository blocks,
    IUnitOfWork unitOfWork
) : ICommandHandler<ApplyBlockOpsCommand, Result<IReadOnlyList<BlockOpResultDto>>>
{
    public async Task<Result<IReadOnlyList<BlockOpResultDto>>> Handle(ApplyBlockOpsCommand command, CancellationToken cancellationToken)
    {
        var context = await PageAccess.RequireWriteAsync(command.PageId, currentUserAccessor, notebooks, pages, cancellationToken);
        if (context.Error is { } error)
        {
            return Result.Failure<IReadOnlyList<BlockOpResultDto>>(error);
        }

        var (_, page, _, _) = context.Value!;

        // The pipeline validator requires 1..100 ops; a direct caller with an empty batch gets a
        // no-op success rather than a pointless lock.
        if (command.Ops.Count == 0)
        {
            return Result.Success<IReadOnlyList<BlockOpResultDto>>([]);
        }

        // Every non-empty batch locks the page row, even a payload-only one: making the lock
        // conditional on the op mix would couple the caller's batch composition to lock ordering,
        // and one FOR UPDATE per batch is cheap. Lock FIRST, then read the chain, inside one
        // explicit transaction so the lock survives until commit.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await blocks.LockPageAsync(command.PageId, cancellationToken);
        var working = (await blocks.ListByPageAsync(command.PageId, cancellationToken)).ToList();
        var tempIds = new Dictionary<string, Block>(StringComparer.Ordinal);

        var results = new List<BlockOpResultDto>(command.Ops.Count);
        foreach (var op in command.Ops)
        {
            var outcome = op.Kind switch
            {
                BlockOpKind.Insert => ApplyInsert(op, page, working, tempIds, command.DryRun),
                BlockOpKind.Update => ApplyUpdate(op, working, tempIds),
                BlockOpKind.Move => ApplyMove(op, page, working, tempIds),
                BlockOpKind.Delete => ApplyDelete(op, page, working, tempIds, command.DryRun),
                _ => Result.Failure<BlockOpResultDto>(BlockErrors.InvalidBlockPayload),
            };
            if (!outcome.IsSuccess)
            {
                // All-or-nothing: the transaction is disposed without a commit, rolling back every
                // op that already ran.
                return Result.Failure<IReadOnlyList<BlockOpResultDto>>(outcome.Error!);
            }

            results.Add(outcome.Value!);
        }

        if (!command.DryRun)
        {
            page.Touch();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        // DryRun: no SaveChanges and no commit — the transaction rolls back on dispose and the
        // tracked-entity mutations are discarded with the context, so the run leaves no trace.

        return Result.Success<IReadOnlyList<BlockOpResultDto>>(results);
    }

    private Result<BlockOpResultDto> ApplyInsert(
        BlockOp op,
        Page page,
        List<Block> working,
        Dictionary<string, Block> tempIds,
        bool dryRun
    )
    {
        if (op.TempId is not null && tempIds.ContainsKey(op.TempId))
        {
            return Result.Failure<BlockOpResultDto>(BlockErrors.DuplicateTempId);
        }

        var normalized = BlockPayloads.ValidateAndNormalize(op.Type ?? string.Empty, op.Content ?? default);
        if (!normalized.IsSuccess)
        {
            return Result.Failure<BlockOpResultDto>(normalized.Error!);
        }

        var position = BlockPositions.ResolveInsertion(op.After, page, working, tempIds, moving: null);
        if (!position.IsSuccess)
        {
            return Result.Failure<BlockOpResultDto>(position.Error!);
        }

        var (parent, group, insertIndex) = position.Value!;
        var block = Block.Create(
            page.Id,
            parent?.Id,
            op.Type!,
            normalized.Value!.CanonicalJson,
            normalized.Value.PlainText,
            BlockSiblingSortKeys.KeyForInsert(group, insertIndex)
        );
        BlockChain.Insert(
            block,
            page,
            parent,
            insertIndex > 0 ? group[insertIndex - 1] : null,
            insertIndex < group.Count ? group[insertIndex] : null
        );
        working.Add(block);
        if (op.TempId is not null)
        {
            tempIds.Add(op.TempId, block);
        }

        // DryRun runs the identical pipeline but issues no repository writes.
        if (!dryRun)
        {
            blocks.Add(block);
        }

        return Result.Success(new BlockOpResultDto(op.TempId, block.Id, block.Revision, CanonicalContent(normalized.Value!)));
    }

    private static Result<BlockOpResultDto> ApplyUpdate(BlockOp op, List<Block> working, IReadOnlyDictionary<string, Block> tempIds)
    {
        if (op.BlockId is null || BlockPositions.ResolveReference(op.BlockId, working, tempIds) is not { } block)
        {
            return Result.Failure<BlockOpResultDto>(BlockErrors.NotFound);
        }

        // BaseRevision is compared to the revision at THIS point in the batch, not at batch
        // start: earlier ops may already have touched the block. A missing BaseRevision can never
        // match, so it fails here even when the pipeline validator was bypassed — never
        // last-write-wins.
        if (block.Revision != op.BaseRevision)
        {
            return Result.Failure<BlockOpResultDto>(BlockErrors.RevisionConflict);
        }

        // Updates normalize against the block's STORED type: the payload contract belongs to the
        // type that produced the row, and writes never change a block's type.
        var normalized = BlockPayloads.ValidateAndNormalize(block.Type, op.Content ?? default);
        if (!normalized.IsSuccess)
        {
            return Result.Failure<BlockOpResultDto>(normalized.Error!);
        }

        block.UpdateContent(normalized.Value!.CanonicalJson, normalized.Value.PlainText);
        return Result.Success(new BlockOpResultDto(null, block.Id, block.Revision, CanonicalContent(normalized.Value!)));
    }

    private static Result<BlockOpResultDto> ApplyMove(BlockOp op, Page page, List<Block> working, IReadOnlyDictionary<string, Block> tempIds)
    {
        if (op.BlockId is null || BlockPositions.ResolveReference(op.BlockId, working, tempIds) is not { } block)
        {
            return Result.Failure<BlockOpResultDto>(BlockErrors.NotFound);
        }

        var position = BlockPositions.ResolveInsertion(op.After, page, working, tempIds, block);
        if (!position.IsSuccess)
        {
            return Result.Failure<BlockOpResultDto>(position.Error!);
        }

        var (parent, group, insertIndex) = position.Value!;
        BlockChain.Move(block, page, parent, working, group, insertIndex);
        return Result.Success(new BlockOpResultDto(null, block.Id, block.Revision));
    }

    private Result<BlockOpResultDto> ApplyDelete(
        BlockOp op,
        Page page,
        List<Block> working,
        Dictionary<string, Block> tempIds,
        bool dryRun
    )
    {
        if (op.BlockId is null || BlockPositions.ResolveReference(op.BlockId, working, tempIds) is not { } block)
        {
            return Result.Failure<BlockOpResultDto>(BlockErrors.NotFound);
        }

        var doomed = BlockChain.DeleteSubtree(block, page, working);
        foreach (var dead in doomed)
        {
            working.Remove(dead);
        }

        // TempIds minted for now-deleted blocks must stop resolving, so a later op referencing
        // one fails like any other reference to a deleted block.
        foreach (var key in tempIds.Where(pair => doomed.Contains(pair.Value)).Select(pair => pair.Key).ToList())
        {
            tempIds.Remove(key);
        }

        if (!dryRun)
        {
            blocks.RemoveRange(doomed);
        }

        return Result.Success(new BlockOpResultDto(null, block.Id, null));
    }

    // The canonical payload round-trips to JsonElement so the DTO hands the response serializer
    // a live document, same as BlockMapping.ToDto.
    private static JsonElement CanonicalContent(NormalizedBlockPayload normalized)
        => JsonSerializer.Deserialize<JsonElement>(normalized.CanonicalJson);
}
