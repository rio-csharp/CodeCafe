using CodeCafe.Domain.Pages;

namespace CodeCafe.Domain.Blocks;

// The sibling chains are a cross-entity structure: the head lives on the page (FirstBlockId) at
// the top level or on the parent block (FirstChildId) when nested, and each link is a
// neighbour's NextSiblingId. These operations keep that structure consistent, so handlers never
// fix up chain pointers themselves. Mirrors PageChain.
public static class BlockChain
{
    // True when newParent is the block itself or sits in the block's subtree, so moving the
    // block under it would create a cycle. Walks ParentBlockId over the page's blocks.
    public static bool IsSelfOrDescendant(Block block, Block newParent, IReadOnlyList<Block> pageBlocks)
    {
        // The visited set keeps a corrupted (cyclic) data set from hanging the walk.
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

    // Detaches the block from its current chain: the predecessor skips over it, or the chain
    // head (Page.FirstBlockId at the top level, the parent's FirstChildId when nested) passes
    // to the block's next sibling. Any list containing the old chain (e.g. every block of the
    // page) locates the predecessor.
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

    // Attaches the block into a chain: right after `after`, or at the head when `after` is null.
    // The head lives on the parent (FirstChildId) or the page (FirstBlockId).
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

    // The sibling group of parentId (null = the page's top level) in chain order, walked from
    // the head pointer (Page.FirstBlockId at the top level, the parent's FirstChildId when
    // nested) along NextSiblingId. Reads order by SortKey; this walk exists for writes, which
    // must take their positions from the structural truth. The visited set keeps a corrupted
    // (cyclic) chain from hanging the walk, mirroring IsSelfOrDescendant.
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

    // Splices a fresh block into a chain: the head pointer (or the predecessor's NextSiblingId)
    // takes the block, and the block takes `next`. Fresh blocks need no Unlink — they are not in
    // any chain yet. Callers must go through here rather than the internal setters, keeping
    // BlockChain the single writer of the chains.
    public static void Insert(Block block, Page page, Block? parent, Block? after, Block? next)
    {
        Link(block, page, parent, after);
        block.SetNextSibling(next?.Id);
    }

    // Full move: detach from the old chain, attach into the new one at insertIndex, re-key, and
    // bump the moved block's Revision. Blocks never leave their page and never move under
    // themselves or a descendant — enforced here even though callers check first. Unlinking
    // comes first so that, for a same-parent move where the two chains are one, the pointer
    // fixup stays correct. The block's own FirstChildId is untouched: the child chain moves
    // with it. `newSiblings` excludes the moving block.
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
        // KeyForInsert (not a bare Between) keeps the rebalance threshold discipline, mirroring
        // PageChain.Move: tight keys re-deal the whole level instead of growing without bound.
        block.MoveTo(newParent?.Id, BlockSiblingSortKeys.KeyForInsert(newSiblings, insertIndex), next?.Id);
        block.MarkMoved();
    }

    // Prepares a hard delete of the subtree rooted at `root`. FirstChildId/NextSiblingId FKs are
    // ON DELETE RESTRICT, so every chain pointer referencing or inside the subtree must be
    // cleared before any row goes away: the root is unlinked from its external chain, then
    // every doomed node loses its own pointers. ParentBlockId cascades, but the cascade never
    // repairs sibling pointers — that is what this pass is for. Returns the doomed blocks in
    // BFS order, root first.
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
