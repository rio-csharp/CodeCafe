using CodeCafe.DbSync.Infrastructure;

namespace CodeCafe.DbSync.Tests;

/// <summary>
/// These values are interpolated into remote shell command lines, so a value that escapes its quoting
/// becomes command execution on a production host. The tool had no tests at all.
/// </summary>
public sealed class ShellEscapingTests
{
    [Fact]
    public void SingleQuote_WrapsPlainValues()
    {
        Assert.Equal("'codecafe'", ShellEscaping.SingleQuote("codecafe"));
    }

    [Fact]
    public void SingleQuote_NeutralizesAnEmbeddedSingleQuote()
    {
        // The classic break-out attempt: a bare ' would close the quoting and expose the rest.
        var escaped = ShellEscaping.SingleQuote("it's");

        Assert.Equal("'it'\"'\"'s'", escaped);
    }

    [Theory]
    [InlineData("; rm -rf /")]
    [InlineData("$(whoami)")]
    [InlineData("`whoami`")]
    [InlineData("&& curl evil.example")]
    [InlineData("| tee /tmp/x")]
    [InlineData("$HOME")]
    [InlineData("a\nb")]
    public void SingleQuote_LeavesShellMetacharactersInsideTheQuotes(string dangerous)
    {
        var escaped = ShellEscaping.SingleQuote(dangerous);

        // Inside single quotes the shell expands nothing, so the payload must simply be
        // wrapped verbatim with no quote characters introduced into the middle.
        Assert.Equal($"'{dangerous}'", escaped);
    }

    [Fact]
    public void SingleQuote_HandlesAValueThatIsOnlyQuotes()
    {
        var escaped = ShellEscaping.SingleQuote("''");

        Assert.Equal("''\"'\"''\"'\"''", escaped);
    }

    [Fact]
    public void SingleQuote_HandlesEmptyInput()
    {
        Assert.Equal("''", ShellEscaping.SingleQuote(string.Empty));
    }

    [Theory]
    [InlineData("a'b")]
    [InlineData("a''b")]
    [InlineData("'")]
    [InlineData("'''")]
    [InlineData("a'b'c'd")]
    [InlineData("; rm -rf / #'")]
    [InlineData("$(id)'$(id)")]
    [InlineData("")]
    [InlineData("plain")]
    public void SingleQuote_RoundTripsThroughPosixQuotingRules(string value)
    {
        // The real invariant is not a character count but that a POSIX shell reads the escaped form
        // back as exactly one word equal to the input. Interpret it here with the same rules the
        // shell uses so a break-out would show up as a mismatch or an unterminated quote.
        var escaped = ShellEscaping.SingleQuote(value);

        Assert.Equal(value, InterpretSingleWord(escaped));
    }

    /// <summary>
    /// Minimal POSIX word interpreter covering exactly the constructs this escaping can produce:
    /// single-quoted spans (no escapes inside) and double-quoted spans. Throws when the input would
    /// not parse as one word, which is the failure mode a quoting bug produces.
    /// </summary>
    private static string InterpretSingleWord(string escaped)
    {
        var result = new System.Text.StringBuilder();
        var index = 0;

        while (index < escaped.Length)
        {
            var character = escaped[index];
            if (character == '\'')
            {
                var end = escaped.IndexOf('\'', index + 1);
                Assert.True(end > 0, $"Unterminated single quote in: {escaped}");
                result.Append(escaped, index + 1, end - index - 1);
                index = end + 1;
                continue;
            }

            if (character == '"')
            {
                var end = escaped.IndexOf('"', index + 1);
                Assert.True(end > 0, $"Unterminated double quote in: {escaped}");
                result.Append(escaped, index + 1, end - index - 1);
                index = end + 1;
                continue;
            }

            // Any bare character outside quotes means the value escaped its quoting: the shell would
            // treat it as syntax (or a separate word) rather than data.
            Assert.Fail($"Unquoted character '{character}' at index {index} in: {escaped}");
        }

        return result.ToString();
    }
}
