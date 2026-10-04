using CodeCafe.Infrastructure.Persistence;

namespace CodeCafe.Infrastructure.Tests.Persistence;

public sealed class LikePatternsTests
{
    [Fact]
    public void Substring_WrapsInWildcards()
    {
        Assert.Equal("%notes%", LikePatterns.Substring("notes"));
    }

    [Theory]
    // LIKE metacharacters must be escaped so user input stays a literal substring match.
    [InlineData("100%", "%100\\%%")]
    [InlineData("a_b", "%a\\_b%")]
    [InlineData("a\\b", "%a\\\\b%")]
    [InlineData("50%_off\\", "%50\\%\\_off\\\\%")]
    public void Substring_EscapesLikeMetacharacters(string input, string expected)
    {
        Assert.Equal(expected, LikePatterns.Substring(input));
    }
}
