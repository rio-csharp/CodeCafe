using CodeCafe.Application.Pages.CreatePage;
using CodeCafe.Application.Pages.UpdatePage;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests.Pages;

public sealed class PageCommandValidatorTests
{
    private readonly CreatePageCommandValidator _createValidator = new();
    private readonly UpdatePageCommandValidator _updateValidator = new();

    [Fact]
    public void Valid_CreateCommand_Passes()
    {
        var result = _createValidator.Validate(new CreatePageCommand("my-notebook", "Getting Started", null));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_Title_FailsCreate(string title)
    {
        var result = _createValidator.Validate(new CreatePageCommand("my-notebook", title, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreatePageCommand.Title));
    }

    [Fact]
    public void Oversized_Title_FailsCreate()
    {
        var title = new string('a', Page.MaxTitleLength + 1);

        var result = _createValidator.Validate(new CreatePageCommand("my-notebook", title, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Null_Title_PassesUpdate()
    {
        // Null means "keep the current title", so only a provided title is validated.
        var result = _updateValidator.Validate(new UpdatePageCommand(Guid.NewGuid(), null, IsArchived: true));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_Title_FailsUpdate(string title)
    {
        var result = _updateValidator.Validate(new UpdatePageCommand(Guid.NewGuid(), title, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdatePageCommand.Title));
    }

    [Fact]
    public void Oversized_Title_FailsUpdate()
    {
        var title = new string('a', Page.MaxTitleLength + 1);

        var result = _updateValidator.Validate(new UpdatePageCommand(Guid.NewGuid(), title, null));

        Assert.False(result.IsValid);
    }
}
