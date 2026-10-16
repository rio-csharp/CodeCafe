using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Domain.Tests.Blocks;

public sealed class BlockChainTests
{
    private static readonly Guid NotebookId = Guid.NewGuid();

    private static Page NewPage() => Page.Create(NotebookId, null, "Page", "page", "a");

    private static Block NewBlock(Page page, Block? parent = null, string sortKey = "a")
        => Block.Create(page.Id, parent?.Id, "paragraph", """{"spans":[]}""", string.Empty, sortKey);

    [Fact]
    public void Link_AfterSibling_PointsPredecessorForward()
    {
        var page = NewPage();
        var a = NewBlock(page);
        var b = NewBlock(page);

        BlockChain.Link(b, page, parent: null, after: a);

        Assert.Equal(b.Id, a.NextSiblingId);
        Assert.Null(page.FirstBlockId);
    }

    [Fact]
    public void Link_AtRootHead_UpdatesPageFirstBlock()
    {
        var page = NewPage();
        var a = NewBlock(page);

        BlockChain.Link(a, page, parent: null, after: null);

        Assert.Equal(a.Id, page.FirstBlockId);
    }

    [Fact]
    public void Link_AtChildChainHead_UpdatesParentFirstChild()
    {
        var page = NewPage();
        var parent = NewBlock(page);
        var child = NewBlock(page, parent);

        BlockChain.Link(child, page, parent, after: null);

        Assert.Equal(child.Id, parent.FirstChildId);
    }

    [Fact]
    public void Unlink_MiddleSibling_PredecessorSkipsOverIt()
    {
        var page = NewPage();
        var a = NewBlock(page);
        var b = NewBlock(page);
        var c = NewBlock(page);
        BlockChain.Link(a, page, parent: null, after: null);
        BlockChain.Link(b, page, parent: null, after: a);
        BlockChain.Link(c, page, parent: null, after: b);

        BlockChain.Unlink(b, page, [a, b, c]);

        Assert.Equal(c.Id, a.NextSiblingId);
    }

    [Fact]
    public void Unlink_RootHead_PageHeadPassesToNextSibling()
    {
        var page = NewPage();
        var a = NewBlock(page);
        var b = NewBlock(page);
        BlockChain.Link(a, page, parent: null, after: null);
        BlockChain.Link(b, page, parent: null, after: a);

        BlockChain.Unlink(a, page, [a, b]);

        Assert.Equal(b.Id, page.FirstBlockId);
    }

    [Fact]
    public void Unlink_ChildChainHead_ParentHeadPassesToNextSibling()
    {
        var page = NewPage();
        var parent = NewBlock(page);
        var a = NewBlock(page, parent);
        var b = NewBlock(page, parent);
        BlockChain.Link(a, page, parent, after: null);
        BlockChain.Link(b, page, parent, after: a);

        BlockChain.Unlink(a, page, [parent, a, b]);

        Assert.Equal(b.Id, parent.FirstChildId);
    }

    [Fact]
    public void Move_ToAnotherParent_FixesBothChainsAndBumpsVersion()
    {
        var page = NewPage();
        var a = NewBlock(page, sortKey: "a");
        var b = NewBlock(page, sortKey: "b");
        var p = NewBlock(page, sortKey: "p");
        var c = NewBlock(page, p, "a");
        BlockChain.Link(a, page, parent: null, after: null);
        BlockChain.Link(b, page, parent: null, after: a);
        BlockChain.Link(p, page, parent: null, after: b);
        BlockChain.Link(c, page, p, after: null);

        // Move b under p, after c.
        BlockChain.Move(b, page, p, pageBlocks: [a, b, p, c], newSiblings: [c], insertIndex: 1);

        Assert.Equal(p.Id, b.ParentBlockId);
        Assert.Equal(p.Id, a.NextSiblingId);
        Assert.Equal(b.Id, c.NextSiblingId);
        Assert.Null(b.NextSiblingId);
        Assert.True(string.CompareOrdinal(c.SortKey, b.SortKey) < 0);
        Assert.Equal(2, b.Version);
        Assert.Equal(1, c.Version);
    }

    [Fact]
    public void Move_WithinSameParent_Reorders()
    {
        var page = NewPage();
        var a = NewBlock(page, sortKey: "a");
        var b = NewBlock(page, sortKey: "b");
        var c = NewBlock(page, sortKey: "c");
        BlockChain.Link(a, page, parent: null, after: null);
        BlockChain.Link(b, page, parent: null, after: a);
        BlockChain.Link(c, page, parent: null, after: b);

        // Move c between a and b.
        BlockChain.Move(c, page, newParent: null, pageBlocks: [a, b, c], newSiblings: [a, b], insertIndex: 1);

        Assert.Null(b.NextSiblingId);
        Assert.Equal(c.Id, a.NextSiblingId);
        Assert.Equal(b.Id, c.NextSiblingId);
        Assert.True(string.CompareOrdinal(a.SortKey, c.SortKey) < 0);
        Assert.True(string.CompareOrdinal(c.SortKey, b.SortKey) < 0);
        Assert.Equal(2, c.Version);
    }

    [Fact]
    public void Move_TightTargetKey_RebalancesTheTargetLevel()
    {
        var page = NewPage();
        var a = NewBlock(page, sortKey: "a");
        var parent = NewBlock(page, sortKey: "p");
        var tail = NewBlock(page, sortKey: new string('z', 60));
        var mover = NewBlock(page, parent, "a");
        BlockChain.Link(a, page, parent: null, after: null);
        BlockChain.Link(parent, page, parent: null, after: a);
        BlockChain.Link(tail, page, parent: null, after: parent);
        BlockChain.Link(mover, page, parent, after: null);

        // Appending after a 60-char key leaves Between no room; like PageChain.Move, the move
        // re-deals the whole target level evenly instead of growing keys without bound.
        BlockChain.Move(mover, page, newParent: null, pageBlocks: [a, parent, tail, mover], newSiblings: [a, parent, tail], insertIndex: 3);

        Assert.All(new[] { a.SortKey, parent.SortKey, tail.SortKey, mover.SortKey }, sortKey => Assert.InRange(sortKey.Length, 1, 2));
        Assert.True(string.CompareOrdinal(a.SortKey, parent.SortKey) < 0);
        Assert.True(string.CompareOrdinal(parent.SortKey, tail.SortKey) < 0);
        Assert.True(string.CompareOrdinal(tail.SortKey, mover.SortKey) < 0);
        Assert.Null(mover.ParentBlockId);
        Assert.Equal(mover.Id, tail.NextSiblingId);
        Assert.Null(parent.FirstChildId); // the old chain was repaired too
        Assert.Equal(2, mover.Version);
        Assert.Equal(1, tail.Version); // re-keyed neighbours are not "updated"
    }

    [Fact]
    public void Move_UnderSelf_Throws()
    {
        var page = NewPage();
        var block = NewBlock(page);

        Assert.Throws<ArgumentException>(
            () => BlockChain.Move(block, page, block, pageBlocks: [block], newSiblings: [], insertIndex: 0));
    }

    [Fact]
    public void Move_UnderOwnDescendant_Throws()
    {
        var page = NewPage();
        var block = NewBlock(page);
        var child = NewBlock(page, block);
        var grandchild = NewBlock(page, child);

        Assert.Throws<ArgumentException>(
            () => BlockChain.Move(block, page, grandchild, pageBlocks: [block, child, grandchild], newSiblings: [], insertIndex: 0));
    }

    [Fact]
    public void Move_AcrossPages_Throws()
    {
        var page = NewPage();
        var otherPage = NewPage();
        var block = NewBlock(page);
        var foreignParent = NewBlock(otherPage);

        Assert.Throws<ArgumentException>(
            () => BlockChain.Move(block, page, foreignParent, pageBlocks: [block], newSiblings: [], insertIndex: 0));
    }

    [Fact]
    public void IsSelfOrDescendant_DetectsSelfAndDescendants()
    {
        var page = NewPage();
        var block = NewBlock(page);
        var child = NewBlock(page, block);
        var unrelated = NewBlock(page);

        Assert.True(BlockChain.IsSelfOrDescendant(block, block, [block]));
        Assert.True(BlockChain.IsSelfOrDescendant(block, child, [block, child]));
        Assert.False(BlockChain.IsSelfOrDescendant(block, unrelated, [block, child, unrelated]));
    }

    [Fact]
    public void DeleteSubtree_ClearsEveryPointerInsideAndIntoTheSubtree()
    {
        var page = NewPage();
        var predecessor = NewBlock(page);
        var root = NewBlock(page);
        var successor = NewBlock(page);
        var child = NewBlock(page, root);
        var sibling = NewBlock(page, root);
        var grandchild = NewBlock(page, child);
        BlockChain.Link(predecessor, page, parent: null, after: null);
        BlockChain.Link(root, page, parent: null, after: predecessor);
        BlockChain.Link(successor, page, parent: null, after: root);
        BlockChain.Link(child, page, root, after: null);
        BlockChain.Link(sibling, page, root, after: child);
        BlockChain.Link(grandchild, page, child, after: null);

        var pageBlocks = new List<Block> { predecessor, root, successor, child, sibling, grandchild };
        var doomed = BlockChain.DeleteSubtree(root, page, pageBlocks);

        // Complete removal set, root first.
        Assert.Equal(root, doomed[0]);
        Assert.Equal(
            new HashSet<Guid> { root.Id, child.Id, sibling.Id, grandchild.Id },
            doomed.Select(block => block.Id).ToHashSet());

        // The external chain is repaired: the predecessor skips over the subtree root.
        Assert.Equal(successor.Id, predecessor.NextSiblingId);
        Assert.Equal(predecessor.Id, page.FirstBlockId);
        Assert.Null(successor.NextSiblingId);

        // Every doomed node lost every chain pointer, so RESTRICT FKs have nothing to reject.
        foreach (var block in doomed)
        {
            Assert.Null(block.NextSiblingId);
            Assert.Null(block.FirstChildId);
        }

        // Survivors keep their own pointers.
        Assert.Null(successor.FirstChildId);
    }

    [Fact]
    public void DeleteSubtree_RootHead_PageHeadPassesToNextSibling()
    {
        var page = NewPage();
        var root = NewBlock(page);
        var successor = NewBlock(page);
        var child = NewBlock(page, root);
        BlockChain.Link(root, page, parent: null, after: null);
        BlockChain.Link(successor, page, parent: null, after: root);
        BlockChain.Link(child, page, root, after: null);

        BlockChain.DeleteSubtree(root, page, [root, successor, child]);

        Assert.Equal(successor.Id, page.FirstBlockId);
    }

    [Fact]
    public void DeleteSubtree_NestedHead_ParentHeadPassesToNextSibling()
    {
        var page = NewPage();
        var parent = NewBlock(page);
        var root = NewBlock(page, parent);
        var successor = NewBlock(page, parent);
        var grandchild = NewBlock(page, root);
        BlockChain.Link(root, page, parent, after: null);
        BlockChain.Link(successor, page, parent, after: root);
        BlockChain.Link(grandchild, page, root, after: null);

        BlockChain.DeleteSubtree(root, page, [parent, root, successor, grandchild]);

        Assert.Equal(successor.Id, parent.FirstChildId);
        Assert.Null(root.NextSiblingId);
        Assert.Null(grandchild.NextSiblingId);
    }

    [Fact]
    public void OrderByChain_TopLevel_WalksFromPageHeadAlongNextSibling()
    {
        var page = NewPage();
        var a = NewBlock(page);
        var b = NewBlock(page);
        var c = NewBlock(page);
        BlockChain.Link(a, page, parent: null, after: null);
        BlockChain.Link(b, page, parent: null, after: a);
        BlockChain.Link(c, page, parent: null, after: b);

        // The input order is deliberately scrambled: only the chain decides.
        var ordered = BlockChain.OrderByChain(page, [c, a, b], parentId: null);

        Assert.Equal([a, b, c], ordered);
    }

    [Fact]
    public void OrderByChain_Nested_WalksFromParentFirstChild()
    {
        var page = NewPage();
        var parent = NewBlock(page);
        var a = NewBlock(page, parent);
        var b = NewBlock(page, parent);
        var outsider = NewBlock(page);
        BlockChain.Link(a, page, parent, after: null);
        BlockChain.Link(b, page, parent, after: a);

        var ordered = BlockChain.OrderByChain(page, [parent, b, outsider, a], parent.Id);

        Assert.Equal([a, b], ordered);
    }

    [Fact]
    public void OrderByChain_CorruptedCycle_TerminatesInsteadOfHanging()
    {
        var page = NewPage();
        var a = NewBlock(page);
        var b = NewBlock(page);
        BlockChain.Link(a, page, parent: null, after: null);
        BlockChain.Link(b, page, parent: null, after: a);
        BlockChain.Link(a, page, parent: null, after: b); // corrupt the chain into a cycle

        var ordered = BlockChain.OrderByChain(page, [a, b], parentId: null);

        Assert.Equal([a, b], ordered);
    }

    [Fact]
    public void Insert_AtHead_HeadPointsToBlockAndBlockToOldHead()
    {
        var page = NewPage();
        var old = NewBlock(page);
        BlockChain.Link(old, page, parent: null, after: null);
        var fresh = NewBlock(page);

        BlockChain.Insert(fresh, page, parent: null, after: null, next: old);

        Assert.Equal(fresh.Id, page.FirstBlockId);
        Assert.Equal(old.Id, fresh.NextSiblingId);
    }

    [Fact]
    public void Insert_BetweenSiblings_PredecessorAndBlockPointForward()
    {
        var page = NewPage();
        var a = NewBlock(page);
        var c = NewBlock(page);
        BlockChain.Link(a, page, parent: null, after: null);
        BlockChain.Link(c, page, parent: null, after: a);
        var fresh = NewBlock(page);

        BlockChain.Insert(fresh, page, parent: null, after: a, next: c);

        Assert.Equal(fresh.Id, a.NextSiblingId);
        Assert.Equal(c.Id, fresh.NextSiblingId);
    }

    [Fact]
    public void Insert_AtChildChainHead_ParentHeadPointsToBlock()
    {
        var page = NewPage();
        var parent = NewBlock(page);
        var fresh = NewBlock(page, parent);

        BlockChain.Insert(fresh, page, parent, after: null, next: null);

        Assert.Equal(fresh.Id, parent.FirstChildId);
        Assert.Null(fresh.NextSiblingId);
    }

    [Fact]
    public void RebuildStructure_RedealsEveryPointer_FromPlacements()
    {
        var page = NewPage();
        var a = NewBlock(page, sortKey: "a");
        var b = NewBlock(page, sortKey: "b");
        var child = NewBlock(page, b, sortKey: "a");
        // Deliberately wrong starting pointers: the rebuild must not trust any of them.
        BlockChain.Link(a, page, parent: null, after: null);
        BlockChain.Link(child, page, parent: null, after: a);
        var placements = new Dictionary<Guid, BlockPlacement>
        {
            [a.Id] = new(null, "a"),
            [b.Id] = new(null, "b"),
            [child.Id] = new(b.Id, "a"),
        };

        BlockChain.RebuildStructure(page, [a, b, child], placements);

        Assert.Equal(a.Id, page.FirstBlockId);
        Assert.Equal(b.Id, a.NextSiblingId);
        Assert.Null(b.NextSiblingId);
        Assert.Equal(child.Id, b.FirstChildId);
        Assert.Null(a.FirstChildId);
        Assert.Equal(b.Id, child.ParentBlockId);
    }

    [Fact]
    public void RebuildStructure_DanglingParent_FallsBackToTopLevel()
    {
        var page = NewPage();
        var orphan = NewBlock(page);
        var placements = new Dictionary<Guid, BlockPlacement>
        {
            [orphan.Id] = new(Guid.CreateVersion7(), "a"), // parent not among the blocks
        };

        BlockChain.RebuildStructure(page, [orphan], placements);

        Assert.Null(orphan.ParentBlockId);
        Assert.Equal(orphan.Id, page.FirstBlockId);
    }
}
