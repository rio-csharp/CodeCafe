using CodeCafe.Api.Authentication;
using Microsoft.Extensions.Options;

namespace CodeCafe.UnitTests.Api.Authentication;

public sealed class ConfiguredCredentialValidatorTests
{
    [Fact]
    public void Validate_returns_configured_username_when_credentials_match()
    {
        var validator = CreateValidator();

        var username = validator.Validate("admin", "secret");

        Assert.Equal("admin", username);
    }

    [Fact]
    public void Validate_returns_null_when_username_does_not_match()
    {
        var validator = CreateValidator();

        var username = validator.Validate("other", "secret");

        Assert.Null(username);
    }

    [Fact]
    public void Validate_returns_null_when_password_does_not_match()
    {
        var validator = CreateValidator();

        var username = validator.Validate("admin", "other");

        Assert.Null(username);
    }

    [Fact]
    public void Validate_treats_missing_credentials_as_invalid()
    {
        var validator = CreateValidator();

        var username = validator.Validate(string.Empty, string.Empty);

        Assert.Null(username);
    }

    private static ConfiguredCredentialValidator CreateValidator()
    {
        return new ConfiguredCredentialValidator(Options.Create(new ConfiguredLoginOptions
        {
            Username = "admin",
            Password = "secret",
            JwtSigningKey = "0123456789abcdef0123456789abcdef",
        }));
    }
}
