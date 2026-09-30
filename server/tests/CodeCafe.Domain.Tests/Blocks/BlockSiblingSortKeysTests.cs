using CodeCafe.Domain.Blocks;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Domain.Tests.Blocks;

public sealed class BlockSiblingSortKeysTests
{
    private static readonly Guid PageId = Guid.NewGuid();

    private static Block NewBlock(string sortKey)
        => Block.Create(PageId, null, "paragraph", """{"spans":[]}""", string.Empty, sortKey);

    [Fact]
    public void KeyForInsert_EmptyGroup_ReturnsTheFirstKey()
    {
        var key = BlockSiblingSortKeys.KeyForInsert([], 0);

        Assert.Equal(Domain.Common.SortKeys.First(), key);
    }

    [Fact]
    public void KeyForInsert_BetweenSiblings_LandsStrictlyBetween()
    {
        var a = NewBlock("a");
        var c = NewBlock("c");

        var key = BlockSiblingSortKeys.KeyForInsert([a, c], 1);

        Assert.True(string.CompareOrdinal(a.SortKey, key) < 0);
        Assert.True(string.CompareOrdinal(key, c.SortKey) < 0);
        Assert.Equal("a", a.SortKey); // no rebalance needed: neighbours keep their keys
        Assert.Equal("c", c.SortKey);
    }

    [Fact]
    public void KeyForInsert_AppendsAfterTheLastKey()
    {
        var a = NewBlock("a");

        var key = BlockSiblingSortKeys.KeyForInsert([a], 1);

        Assert.True(string.CompareOrdinal(a.SortKey, key) < 0);
    }

    [Fact]
    public void KeyForInsert_TightKeyBeyondThreshold_RebalancesTheWholeLevel()
    {
        // A key at the cap leaves no room after it, forcing the level to be re-dealt evenly.
        var a = NewBlock("a");
        var b = NewBlock(new string('z', 60));

        var key = BlockSiblingSortKeys.KeyForInsert([a, b], 2);

        Assert.All(new[] { a.SortKey, b.SortKey, key }, sortKey => Assert.InRange(sortKey.Length, 1, 2));
        Assert.True(string.CompareOrdinal(a.SortKey, b.SortKey) < 0);
        Assert.True(string.CompareOrdinal(b.SortKey, key) < 0);
    }
}
