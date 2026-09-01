using CodeCafe.Domain.Notes.ValueObjects;

namespace CodeCafe.Domain.Tests;

public sealed class NotebookSlugTests
{
    [Fact]
    public void Create_Normalizes_To_Lowercase()
    {
        Assert.Equal("getting-started", NotebookSlug.Create("Getting-Started").Value);
    }

    [Fact]
    public void Create_Trims_Whitespace()
    {
        Assert.Equal("my-slug", NotebookSlug.Create("  my-slug  ").Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Rejects_Empty(string? value)
    {
        Assert.Throws<ArgumentException>(() => NotebookSlug.Create(value!));
    }

    [Fact]
    public void Create_Rejects_Over_Max_Length()
    {
        var value = new string('a', NotebookSlug.MaxLength + 1);

        Assert.Throws<ArgumentException>(() => NotebookSlug.Create(value));
    }

    [Fact]
    public void Records_With_Same_Value_Are_Equal()
    {
        Assert.Equal(NotebookSlug.Create("a-b"), NotebookSlug.Create("A-B"));
    }
}
