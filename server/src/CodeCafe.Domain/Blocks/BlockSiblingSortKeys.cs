using CodeCafe.Domain.Common;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Domain.Blocks;

// Re-keying for the block sibling groups, mirroring SiblingSortKeys for pages.
public static class BlockSiblingSortKeys
{
    public const int RebalanceThreshold = SiblingSortKeys.RebalanceThreshold;

    // The caller passes the siblings in CHAIN order, so the rebalance pass inherently walks the
    // chain — the projection is only ever rebuilt from the structural truth, never from an
    // ORDER BY SortKey read. See SiblingSortKeys for why rebalancing inline is safe.
    public static string KeyForInsert(IReadOnlyList<Block> siblings, int insertIndex)
    {
        var before = insertIndex > 0 ? siblings[insertIndex - 1].SortKey : null;
        var after = insertIndex < siblings.Count ? siblings[insertIndex].SortKey : null;
        var key = SortKeys.Between(before, after);
        if (key.Length <= RebalanceThreshold)
        {
            return key;
        }

        var keys = SortKeys.EvenlySpaced(siblings.Count + 1);
        for (var index = 0; index < siblings.Count; index++)
        {
            siblings[index].Rekey(keys[index < insertIndex ? index : index + 1]);
        }

        return keys[insertIndex];
    }
}
