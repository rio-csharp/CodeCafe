using CodeCafe.DbSync.Infrastructure;

namespace CodeCafe.DbSync.Tests;

public sealed class ProcessArgumentsTests
{
    [Fact]
    public void Quote_LeavesSimpleValuesUnquoted()
    {
        Assert.Equal("codecafe", ProcessArguments.Quote("codecafe"));
    }

    [Fact]
    public void Quote_QuotesValuesContainingWhitespace()
    {
        Assert.Equal("\"C:\\\\my path\"", ProcessArguments.Quote(@"C:\my path"));
    }

    [Fact]
    public void Quote_EscapesEmbeddedQuotes()
    {
        // An unescaped quote would end the argument early and let the rest be read as new arguments.
        var quoted = ProcessArguments.Quote("say \"hi\"");

        Assert.Equal("\"say \\\"hi\\\"\"", quoted);
    }

    [Fact]
    public void Quote_RepresentsAnEmptyArgumentExplicitly()
    {
        // Without this an empty argument would vanish and shift every later positional argument.
        Assert.Equal("\"\"", ProcessArguments.Quote(string.Empty));
    }

    [Fact]
    public void Quote_QuotesTabsAndNewlinesAsWhitespace()
    {
        Assert.StartsWith("\"", ProcessArguments.Quote("a\tb"), StringComparison.Ordinal);
        Assert.StartsWith("\"", ProcessArguments.Quote("a\nb"), StringComparison.Ordinal);
    }

    [Fact]
    public void Join_SeparatesArgumentsWithSpaces()
    {
        var joined = ProcessArguments.Join("pg_dump", "--host", "localhost");

        Assert.Equal("pg_dump --host localhost", joined);
    }

    [Fact]
    public void Join_KeepsAnArgumentWithSpacesAsOneToken()
    {
        var joined = ProcessArguments.Join("psql", "--file", @"C:\back ups\dump.sql");

        Assert.Equal("psql --file \"C:\\\\back ups\\\\dump.sql\"", joined);
    }

    [Fact]
    public void Join_PreservesEmptyArgumentPositions()
    {
        var joined = ProcessArguments.Join("tool", string.Empty, "after");

        Assert.Equal("tool \"\" after", joined);
    }
}
