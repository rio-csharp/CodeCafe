using CodeCafe.Domain.Pages;

namespace CodeCafe.Domain.Blocks;

public static class BlockChain
{
    public static bool IsSelfOrDescendant(Block block, Block newParent, IReadOnlyList<Block> pageBlocks)
    {
        var visited = new HashSet<Guid>();
        for (Block? current = newParent; current is not null && visited.Add(current.Id);)
        {
            if (current.Id == block.Id)
            {
                return true;
            }

            current = pageBlocks.FirstOrDefault(candidate => candidate.Id == current.ParentBlockId);
        }

        return false;
    }

    public static void Unlink(Block block, Page page, IReadOnlyList<Block> pageBlocks)
    {
        var predecessor = pageBlocks.FirstOrDefault(candidate => candidate.NextSiblingId == block.Id);
        if (predecessor is not null)
        {
            predecessor.SetNextSibling(block.NextSiblingId);
        }
        else if (block.ParentBlockId is null)
        {
            if (page.FirstBlockId == block.Id)
            {
                page.SetFirstBlock(block.NextSiblingId);
            }
        }
        else
        {
            var parent = pageBlocks.FirstOrDefault(candidate => candidate.Id == block.ParentBlockId);
            if (parent?.FirstChildId == block.Id)
            {
                parent.SetFirstChild(block.NextSiblingId);
            }
        }
    }

    public static void Link(Block block, Page page, Block? parent, Block? after)
    {
        if (after is not null)
        {
            after.SetNextSibling(block.Id);
        }
        else if (parent is not null)
        {
            parent.SetFirstChild(block.Id);
        }
        else
        {
            page.SetFirstBlock(block.Id);
        }
    }

    public static IReadOnlyList<Block> OrderByChain(Page page, IReadOnlyList<Block> pageBlocks, Guid? parentId)
    {
        var headId = parentId is null
            ? page.FirstBlockId
            : pageBlocks.FirstOrDefault(candidate => candidate.Id == parentId)?.FirstChildId;

        var ordered = new List<Block>();
        var visited = new HashSet<Guid>();
        for (Block? current = pageBlocks.FirstOrDefault(candidate => candidate.Id == headId);
             current is not null && visited.Add(current.Id);
             current = pageBlocks.FirstOrDefault(candidate => candidate.Id == current.NextSiblingId))
        {
            ordered.Add(current);
        }

        return ordered;
    }

    public static void Insert(Block block, Page page, Block? parent, Block? after, Block? next)
    {
        Link(block, page, parent, after);
        block.SetNextSibling(next?.Id);
    }

    public static void RebuildStructure(
        Page page,
        IReadOnlyList<Block> blocks,
        IReadOnlyDictionary<Guid, BlockPlacement> placements
    )
    {
        var ids = new HashSet<Guid>(blocks.Select(block => block.Id));
        var childrenOf = new Dictionary<Guid, List<Block>>();
        foreach (var block in blocks)
        {
            var placement = placements[block.Id];
            var parentId = placement.ParentBlockId is { } candidate && ids.Contains(candidate) ? candidate : (Guid?)null;
            block.MoveTo(parentId, placement.SortKey, null);
            block.SetFirstChild(null);
            if (!childrenOf.TryGetValue(parentId ?? Guid.Empty, out var group))
            {
                childrenOf[parentId ?? Guid.Empty] = group = [];
            }

            group.Add(block);
        }

        page.SetFirstBlock(null);
        var byId = blocks.ToDictionary(block => block.Id);
        foreach (var (parentId, group) in childrenOf)
        {
            group.Sort(static (left, right) => string.CompareOrdinal(left.SortKey, right.SortKey));
            for (var index = 0; index < group.Count; index++)
            {
                Link(group[index], page, parentId == Guid.Empty ? null : byId[parentId], index > 0 ? group[index - 1] : null);
                group[index].SetNextSibling(index + 1 < group.Count ? group[index + 1].Id : null);
            }
        }
    }

    public static void Move(
        Block block,
        Page page,
        Block? newParent,
        IReadOnlyList<Block> pageBlocks,
        IReadOnlyList<Block> newSiblings,
        int insertIndex
    )
    {
        if (newParent is not null)
        {
            if (newParent.PageId != block.PageId)
            {
                throw new ArgumentException("Blocks never move across pages.", nameof(newParent));
            }

            if (IsSelfOrDescendant(block, newParent, pageBlocks))
            {
                throw new ArgumentException("A block cannot move under itself or its own descendant.", nameof(newParent));
            }
        }

        Unlink(block, page, pageBlocks);
        Link(block, page, newParent, insertIndex > 0 ? newSiblings[insertIndex - 1] : null);

        var next = insertIndex < newSiblings.Count ? newSiblings[insertIndex] : null;
        block.MoveTo(newParent?.Id, BlockSiblingSortKeys.KeyForInsert(newSiblings, insertIndex), next?.Id);
        block.MarkMoved();
    }

    public static IReadOnlyList<Block> DeleteSubtree(Block root, Page page, IReadOnlyList<Block> pageBlocks)
    {
        var doomed = new List<Block>();
        var queue = new Queue<Block>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            doomed.Add(current);
            foreach (var child in pageBlocks.Where(candidate => candidate.ParentBlockId == current.Id))
            {
                queue.Enqueue(child);
            }
        }

        Unlink(root, page, pageBlocks);

        foreach (var block in doomed)
        {
            block.SetNextSibling(null);
            block.SetFirstChild(null);
        }

        return doomed;
    }
}
