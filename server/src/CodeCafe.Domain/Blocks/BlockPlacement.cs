namespace CodeCafe.Domain.Blocks;

// Where a block sat in the tree at some point in the page's history: its parent (null = top
// level) and its sibling sort key. Revision snapshots feed BlockChain.RebuildStructure these.
public sealed record BlockPlacement(Guid? ParentBlockId, string SortKey);
