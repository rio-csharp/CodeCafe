using CodeCafe.Application.Notes.Commands.ChangeNotebookSlug;

namespace CodeCafe.Application.Tests;

public sealed class ChangeNotebookSlugCommandValidatorTests
{
    private readonly ChangeNotebookSlugCommandValidator _validator = new();

    [Fact]
    public void Valid_Slug_Passes()
    {
        var result = _validator.Validate(
            new ChangeNotebookSlugCommand(Guid.NewGuid(), Guid.NewGuid(), "my-custom-slug")
        );

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("not a slug!")]
    public void Invalid_Slug_Fails(string slug)
    {
        var result = _validator.Validate(
            new ChangeNotebookSlugCommand(Guid.NewGuid(), Guid.NewGuid(), slug)
        );

        Assert.False(result.IsValid);
    }
}
