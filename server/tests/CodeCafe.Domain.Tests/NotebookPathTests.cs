using CodeCafe.Domain.Notes.ValueObjects;

namespace CodeCafe.Domain.Tests;

public sealed class NotebookPathTests
{
    [Fact]
    public void Create_Strips_Surrounding_Slashes()
    {
        Assert.Equal("guides/overview", NotebookPath.Create("/guides/overview/").Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("   ")]
    public void Create_Rejects_Empty(string value)
    {
        Assert.Throws<ArgumentException>(() => NotebookPath.Create(value));
    }

    [Fact]
    public void Create_Rejects_Over_Max_Length()
    {
        var value = new string('a', NotebookPath.MaxLength + 1);

        Assert.Throws<ArgumentException>(() => NotebookPath.Create(value));
    }

    [Fact]
    public void IsDescendantOf_Matches_At_Any_Depth()
    {
        var ancestor = NotebookPath.Create("guides");

        Assert.True(NotebookPath.Create("guides/overview").IsDescendantOf(ancestor));
        Assert.True(NotebookPath.Create("guides/advanced/caching").IsDescendantOf(ancestor));
        Assert.False(NotebookPath.Create("settings").IsDescendantOf(ancestor));
    }

    [Fact]
    public void IsDescendantOf_Does_Not_Match_Sibling_With_Shared_Prefix()
    {
        var ancestor = NotebookPath.Create("guides");

        Assert.False(NotebookPath.Create("guides2/overview").IsDescendantOf(ancestor));
    }

    [Fact]
    public void IsDescendantOf_Returns_False_For_Self()
    {
        var path = NotebookPath.Create("guides");

        Assert.False(path.IsDescendantOf(path));
    }
}
