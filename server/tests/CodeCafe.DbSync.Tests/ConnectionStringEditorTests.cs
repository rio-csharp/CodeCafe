using CodeCafe.DbSync.Infrastructure;

namespace CodeCafe.DbSync.Tests;

public sealed class ConnectionStringEditorTests
{
    [Fact]
    public void WithHostAndPort_ReplacesExistingHostAndPort()
    {
        var result = ConnectionStringEditor.WithHostAndPort(
            "Host=prod.example;Port=5432;Database=codecafe;Username=codecafe",
            "127.0.0.1",
            15432);

        Assert.Contains("Host=127.0.0.1", result, StringComparison.Ordinal);
        Assert.Contains("Port=15432", result, StringComparison.Ordinal);
        Assert.DoesNotContain("prod.example", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Port=5432", result, StringComparison.Ordinal);
    }

    [Fact]
    public void WithHostAndPort_AddsHostAndPortWhenAbsent()
    {
        var result = ConnectionStringEditor.WithHostAndPort(
            "Database=codecafe;Username=codecafe",
            "localhost",
            5433);

        Assert.Contains("Host=localhost", result, StringComparison.Ordinal);
        Assert.Contains("Port=5433", result, StringComparison.Ordinal);
        Assert.Contains("Database=codecafe", result, StringComparison.Ordinal);
    }

    [Fact]
    public void WithHostAndPort_PreservesOtherKeysAndTheirValues()
    {
        var result = ConnectionStringEditor.WithHostAndPort(
            "Host=a;Port=1;Database=codecafe;Username=u;Password=p;Include Error Detail=true",
            "b",
            2);

        Assert.Contains("Database=codecafe", result, StringComparison.Ordinal);
        Assert.Contains("Username=u", result, StringComparison.Ordinal);
        Assert.Contains("Password=p", result, StringComparison.Ordinal);
        Assert.Contains("Include Error Detail=true", result, StringComparison.Ordinal);
    }

    [Fact]
    public void WithHostAndPort_MatchesExistingKeysCaseInsensitively()
    {
        // Npgsql keys are case-insensitive, so "host=" must be replaced rather than duplicated;
        // a duplicated Host would make the resulting string ambiguous.
        var result = ConnectionStringEditor.WithHostAndPort("host=old;PORT=1;Database=d", "new", 2);

        Assert.Equal(1, CountOccurrences(result, "new"));
        Assert.DoesNotContain("old", result, StringComparison.Ordinal);
        Assert.Equal(
            1,
            result.Split(';').Count(part => part.StartsWith("Host=", StringComparison.OrdinalIgnoreCase)));
        Assert.Equal(
            1,
            result.Split(';').Count(part => part.StartsWith("Port=", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void WithHostAndPort_IgnoresMalformedSegments()
    {
        var result = ConnectionStringEditor.WithHostAndPort("Database=d;garbage;=novalue;Port=1", "h", 2);

        Assert.Contains("Database=d", result, StringComparison.Ordinal);
        Assert.DoesNotContain("garbage", result, StringComparison.Ordinal);
        Assert.Contains("Host=h", result, StringComparison.Ordinal);
    }

    [Fact]
    public void MaskPassword_HidesOnlyThePasswordValue()
    {
        var result = ConnectionStringEditor.MaskPassword(
            "Host=h;Port=1;Database=d;Username=u;Password=sup3rs3cret");

        Assert.DoesNotContain("sup3rs3cret", result, StringComparison.Ordinal);
        Assert.Contains("Password=<masked>", result, StringComparison.Ordinal);
        Assert.Contains("Username=u", result, StringComparison.Ordinal);
    }

    [Fact]
    public void MaskPassword_MatchesTheKeyCaseInsensitively()
    {
        var result = ConnectionStringEditor.MaskPassword("Host=h;password=sup3rs3cret");

        Assert.DoesNotContain("sup3rs3cret", result, StringComparison.Ordinal);
    }

    [Fact]
    public void MaskPassword_LeavesStringsWithoutAPasswordUnchanged()
    {
        const string original = "Host=h;Port=1;Database=d";

        Assert.Equal(original, ConnectionStringEditor.MaskPassword(original));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }
}
