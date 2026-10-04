using CodeCafe.Application.Notebooks.ChangeNotebookSlug;
using CodeCafe.Application.Notebooks.CreateNotebook;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Tests.Notebooks;

public sealed class NotebookSlugValidatorTests
{
    private readonly CreateNotebookCommandValidator _createValidator = new();
    private readonly ChangeNotebookSlugCommandValidator _changeValidator = new();

    [Fact]
    public void Padded_Slug_PassesCreate_ValidatedAfterNormalization()
    {
        var result = _createValidator.Validate(
            new CreateNotebookCommand("Title", null, " Custom-Slug ", NotebookVisibility.Private)
        );

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Padded_Slug_PassesChange_ValidatedAfterNormalization()
    {
        var result = _changeValidator.Validate(new ChangeNotebookSlugCommand("my-notebook", " Custom-Slug "));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("-abc")]
    [InlineData("abc-")]
    [InlineData("a--b")]
    public void Unsalvageable_Slug_FailsCreate(string slug)
    {
        var result = _createValidator.Validate(
            new CreateNotebookCommand("Title", null, slug, NotebookVisibility.Private)
        );

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("-abc")]
    [InlineData("abc-")]
    [InlineData("a--b")]
    public void Unsalvageable_Slug_FailsChange(string slug)
    {
        var result = _changeValidator.Validate(new ChangeNotebookSlugCommand("my-notebook", slug));

        Assert.False(result.IsValid);
    }
}
