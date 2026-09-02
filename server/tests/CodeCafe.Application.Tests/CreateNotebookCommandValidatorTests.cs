using CodeCafe.Application.Notes.Commands.CreateNotebook;

namespace CodeCafe.Application.Tests;

public sealed class CreateNotebookCommandValidatorTests
{
    private readonly CreateNotebookCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(new CreateNotebookCommand(Guid.NewGuid(), "My Notes", "desc", "public"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_Title_Fails(string? title)
    {
        var result = _validator.Validate(new CreateNotebookCommand(Guid.NewGuid(), title!, null, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Title_Over_160_Characters_Fails()
    {
        var result = _validator.Validate(
            new CreateNotebookCommand(Guid.NewGuid(), new string('a', 161), null, null)
        );

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Description_Over_1000_Characters_Fails()
    {
        var result = _validator.Validate(
            new CreateNotebookCommand(Guid.NewGuid(), "My Notes", new string('a', 1001), null)
        );

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("5")]
    [InlineData("nonsense")]
    public void Undefined_Visibility_Fails(string visibility)
    {
        var result = _validator.Validate(
            new CreateNotebookCommand(Guid.NewGuid(), "My Notes", null, visibility)
        );

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Valid_Explicit_Slug_Passes()
    {
        var result = _validator.Validate(
            new CreateNotebookCommand(Guid.NewGuid(), "My Notes", null, null, "my-custom-slug")
        );

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("Has Uppercase/Chars")]
    [InlineData("double--dash")]
    [InlineData("-leading-dash")]
    public void Invalid_Explicit_Slug_Fails(string slug)
    {
        var result = _validator.Validate(
            new CreateNotebookCommand(Guid.NewGuid(), "My Notes", null, null, slug)
        );

        Assert.False(result.IsValid);
    }
}
