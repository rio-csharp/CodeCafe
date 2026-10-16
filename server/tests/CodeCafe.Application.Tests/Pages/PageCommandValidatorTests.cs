using CodeCafe.Application.Pages.ChangePageSlug;
using CodeCafe.Application.Pages.CreatePage;
using CodeCafe.Application.Pages.SharePage;
using CodeCafe.Application.Pages.UpdatePage;
using CodeCafe.Domain.Pages;

namespace CodeCafe.Application.Tests.Pages;

public sealed class PageCommandValidatorTests
{
    private readonly CreatePageCommandValidator _createValidator = new();
    private readonly UpdatePageCommandValidator _updateValidator = new();
    private readonly ChangePageSlugCommandValidator _changeSlugValidator = new();
    private readonly SharePageCommandValidator _shareValidator = new();

    [Fact]
    public void Valid_ShareCommand_Passes()
    {
        var result = _shareValidator.Validate(new SharePageCommand(Guid.NewGuid(), "yao@example.com", default));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public void Invalid_Email_FailsShare(string email)
    {
        var result = _shareValidator.Validate(new SharePageCommand(Guid.NewGuid(), email, default));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SharePageCommand.Email));
    }

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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Absent_Slug_PassesChangePageSlug(string? slug)
    {
        // No slug means "derive one from the title", so there is nothing to validate.
        var result = _changeSlugValidator.Validate(new ChangePageSlugCommand(Guid.NewGuid(), slug));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Padded_Slug_PassesChangePageSlug()
    {
        var result = _changeSlugValidator.Validate(new ChangePageSlugCommand(Guid.NewGuid(), "  Filter-Notes  "));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("filter notes")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("double--hyphen")]
    [InlineData("punct!")]
    public void Invalid_Slug_FailsChangePageSlug(string slug)
    {
        var result = _changeSlugValidator.Validate(new ChangePageSlugCommand(Guid.NewGuid(), slug));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ChangePageSlugCommand.Slug));
    }

    [Fact]
    public void Oversized_Slug_FailsChangePageSlug()
    {
        var slug = new string('a', Page.MaxSlugLength + 1);

        var result = _changeSlugValidator.Validate(new ChangePageSlugCommand(Guid.NewGuid(), slug));

        Assert.False(result.IsValid);
    }
}
