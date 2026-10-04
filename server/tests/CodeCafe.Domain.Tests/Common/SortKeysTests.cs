using CodeCafe.Domain.Common;

namespace CodeCafe.Domain.Tests.Common;

public sealed class SortKeysTests
{
    [Fact]
    public void Between_TwoNulls_ReturnsAMiddleKey()
    {
        var key = SortKeys.First();

        Assert.True(string.CompareOrdinal("a", key) < 0);
        Assert.True(string.CompareOrdinal(key, "z") < 0);
    }

    [Fact]
    public void Between_Neighbors_ReturnsAKeyStrictlyBetween()
    {
        var key = SortKeys.Between("a", "b");

        Assert.True(string.CompareOrdinal("a", key) < 0);
        Assert.True(string.CompareOrdinal(key, "b") < 0);
    }

    [Fact]
    public void Between_OpenBounds_StayOnTheRightSide()
    {
        Assert.True(string.CompareOrdinal(SortKeys.Between("m", null), "m") > 0);
        Assert.True(string.CompareOrdinal(SortKeys.Between(null, "m"), "m") < 0);
    }

    [Fact]
    public void Between_TightKeys_GrowsDeeper()
    {
        var key = SortKeys.Between("a", "a1");

        Assert.StartsWith("a", key);
        Assert.True(string.CompareOrdinal("a", key) < 0);
        Assert.True(string.CompareOrdinal(key, "a1") < 0);
    }

    [Fact]
    public void Between_ReversedBounds_Throws() => Assert.Throws<ArgumentException>(() => SortKeys.Between("b", "a"));

    [Fact]
    public void EvenlySpaced_ReturnsOrderedUniqueKeys_WithRoomOnBothSides()
    {
        var keys = SortKeys.EvenlySpaced(3);

        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.Equal(keys.Order(StringComparer.Ordinal), keys);
        Assert.True(string.CompareOrdinal("0", keys[0]) < 0);
        Assert.True(string.CompareOrdinal(keys[^1], "z") < 0);
    }

    [Fact]
    public void EvenlySpaced_WidensToTwoDigits_WhenOneDigitCannotSpaceTheKeys()
    {
        var keys = SortKeys.EvenlySpaced(100);

        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.All(keys, key => Assert.Equal(2, key.Length));
        Assert.Equal(keys.Order(StringComparer.Ordinal), keys);
    }

    [Fact]
    public void EvenlySpaced_Zero_ReturnsEmpty() => Assert.Empty(SortKeys.EvenlySpaced(0));
}
