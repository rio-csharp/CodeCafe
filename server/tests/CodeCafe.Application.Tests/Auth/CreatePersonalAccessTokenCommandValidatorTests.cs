using CodeCafe.Application.Auth.PersonalAccessTokens.CreatePersonalAccessToken;

namespace CodeCafe.Application.Tests.Auth;

public sealed class CreatePersonalAccessTokenCommandValidatorTests
{
    private const string ValidName = "mcp";

    private readonly CreatePersonalAccessTokenCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(new CreatePersonalAccessTokenCommand(ValidName, 30));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Missing_Expiry_Passes()
    {
        var result = _validator.Validate(new CreatePersonalAccessTokenCommand(ValidName, null));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_Name_Fails(string name)
    {
        var result = _validator.Validate(new CreatePersonalAccessTokenCommand(name, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreatePersonalAccessTokenCommand.Name));
    }

    [Fact]
    public void Oversized_Name_Fails()
    {
        var name = new string('a', 51);

        var result = _validator.Validate(new CreatePersonalAccessTokenCommand(name, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreatePersonalAccessTokenCommand.Name));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(366)]
    public void OutOfRange_Expiry_Fails(int expiresInDays)
    {
        var result = _validator.Validate(new CreatePersonalAccessTokenCommand(ValidName, expiresInDays));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreatePersonalAccessTokenCommand.ExpiresInDays));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(365)]
    public void Boundary_Expiry_Passes(int expiresInDays)
    {
        var result = _validator.Validate(new CreatePersonalAccessTokenCommand(ValidName, expiresInDays));

        Assert.True(result.IsValid);
    }
}
