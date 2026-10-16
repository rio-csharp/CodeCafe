using CodeCafe.Application.Common;
using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Blocks.Shared;

// Shared write-side position logic for the block handlers. An After reference (a live block's
// Guid, or a TempId minted by an earlier op in a batch) resolves to a parent group, an ordered
// sibling list in chain order — excluding the moving block — and the insertion index. Null
// After targets the top-level head; otherwise the block lands right after the after-block in
// ITS group. An explicit Parent reference overrides that: After is then interpreted among the
// parent's children, and null After means FIRST child — the only way into a childless parent.
// Lookups run against the caller's working state, so anything deleted earlier in a batch no
// longer resolves.
public static class BlockPositions
{
    public static Block? ResolveReference(
        string reference,
        IReadOnlyList<Block> pageBlocks,
        IReadOnlyDictionary<string, Block>? tempIds
    )
        => Guid.TryParse(reference, out var id)
            ? pageBlocks.FirstOrDefault(block => block.Id == id)
            : tempIds?.GetValueOrDefault(reference);

    public static Result<(Block? Parent, List<Block> Group, int InsertIndex)> ResolveInsertion(
        string? after,
        Page page,
        IReadOnlyList<Block> pageBlocks,
        IReadOnlyDictionary<string, Block>? tempIds,
        Block? moving,
        string? parentRef = null
    )
    {
        static bool IsNotMoving(Block candidate, Block? moving) => moving is null || candidate.Id != moving.Id;

        if (parentRef is not null)
        {
            var parentBlock = ResolveReference(parentRef, pageBlocks, tempIds);
            if (parentBlock is null || parentBlock == moving)
            {
                return Result.Failure<(Block?, List<Block>, int)>(BlockErrors.InvalidBlockPosition);
            }

            // BlockChain.Move throws on cycles; surface the same violation as a friendly Result
            // failure instead — an agent's bad target is input, not a bug.
            if (moving is not null && BlockChain.IsSelfOrDescendant(moving, parentBlock, pageBlocks))
            {
                return Result.Failure<(Block?, List<Block>, int)>(BlockErrors.InvalidBlockPosition);
            }

            var childGroup = BlockChain.OrderByChain(page, pageBlocks, parentBlock.Id)
                .Where(block => IsNotMoving(block, moving))
                .ToList();
            if (after is null)
            {
                return Result.Success<(Block?, List<Block>, int)>((parentBlock, childGroup, 0));
            }

            var afterChild = ResolveReference(after, pageBlocks, tempIds);
            if (afterChild is null || afterChild == moving || afterChild.ParentBlockId != parentBlock.Id)
            {
                return Result.Failure<(Block?, List<Block>, int)>(BlockErrors.InvalidBlockPosition);
            }
            return Result.Success<(Block?, List<Block>, int)>((parentBlock, childGroup, childGroup.IndexOf(afterChild) + 1));
        }

        if (after is null)
        {
            var topLevel = BlockChain.OrderByChain(page, pageBlocks, null).Where(block => IsNotMoving(block, moving)).ToList();
            return Result.Success<(Block?, List<Block>, int)>((null, topLevel, 0));
        }

        var afterBlock = ResolveReference(after, pageBlocks, tempIds);
        if (afterBlock is null || afterBlock == moving)
        {
            return Result.Failure<(Block?, List<Block>, int)>(BlockErrors.InvalidBlockPosition);
        }

        var parent = afterBlock.ParentBlockId is { } parentId
            ? pageBlocks.FirstOrDefault(block => block.Id == parentId)
            : null;

        // BlockChain.Move throws on cycles; surface the same violation as a friendly Result
        // failure instead — an agent's bad target is input, not a bug.
        if (moving is not null && parent is not null && BlockChain.IsSelfOrDescendant(moving, parent, pageBlocks))
        {
            return Result.Failure<(Block?, List<Block>, int)>(BlockErrors.InvalidBlockPosition);
        }

        var group = BlockChain.OrderByChain(page, pageBlocks, afterBlock.ParentBlockId)
            .Where(block => IsNotMoving(block, moving))
            .ToList();
        return Result.Success<(Block?, List<Block>, int)>((parent, group, group.IndexOf(afterBlock) + 1));
    }
}
