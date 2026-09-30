using CodeCafe.Application.Auth.Register;

namespace CodeCafe.Application.Tests.Auth;

public sealed class RegisterCommandValidatorTests
{
    private const string ValidEmail = "yao@example.com";
    private const string ValidPassword = "Password123!";
    private const string ValidDisplayName = "Yao";

    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(new RegisterCommand(ValidEmail, ValidPassword, ValidDisplayName));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public void Invalid_Email_Fails(string email)
    {
        var result = _validator.Validate(new RegisterCommand(email, ValidPassword, ValidDisplayName));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterCommand.Email));
    }

    [Fact]
    public void Oversized_Email_Fails()
    {
        var email = new string('a', 250) + "@example.com";

        var result = _validator.Validate(new RegisterCommand(email, ValidPassword, ValidDisplayName));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("7chars!")]
    public void Short_Password_Fails(string password)
    {
        var result = _validator.Validate(new RegisterCommand(ValidEmail, password, ValidDisplayName));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public void Oversized_Password_Fails()
    {
        var password = new string('a', 129);

        var result = _validator.Validate(new RegisterCommand(ValidEmail, password, ValidDisplayName));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_DisplayName_Fails(string displayName)
    {
        var result = _validator.Validate(new RegisterCommand(ValidEmail, ValidPassword, displayName));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterCommand.DisplayName));
    }

    [Fact]
    public void Oversized_DisplayName_Fails()
    {
        var displayName = new string('a', 41);

        var result = _validator.Validate(new RegisterCommand(ValidEmail, ValidPassword, displayName));

        Assert.False(result.IsValid);
    }
}
