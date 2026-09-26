using CodeCafe.Domain.Common;

namespace CodeCafe.Domain.Pages;

public static class SiblingSortKeys
{
    // Keys grow one level per ~5 tight insertions; 48 leaves 16 levels (~80 tight inserts) of
    // headroom under the 64-char column, so rebalancing kicks in long before the database would
    // reject a key.
    public const int RebalanceThreshold = 48;

    // The slot the incoming page takes among the SortKey-ordered siblings. When the tight key
    // would exceed the threshold, the whole level is re-dealt evenly instead. Rebalancing only
    // rewrites SortKey (the read projection) and never touches the chains, so it stays safe to
    // run inline in the request; a rare race can misplace a key, but the chains remain the truth
    // and the next rebalance repairs the projection.
    public static string KeyForInsert(IReadOnlyList<Page> siblings, int insertIndex)
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
