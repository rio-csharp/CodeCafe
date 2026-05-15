using CodeCafe.Application.Auth;

namespace CodeCafe.UnitTests.Application.Auth;

public sealed class AuthServiceTests
{
    [Fact]
    public void SignIn_returns_null_when_credentials_are_invalid()
    {
        var service = new AuthService(
            new StubCredentialValidator(null),
            new StubAuthTokenIssuer());

        var result = service.SignIn("admin", "bad");

        Assert.Null(result);
    }

    [Fact]
    public void SignIn_issues_token_when_credentials_are_valid()
    {
        var service = new AuthService(
            new StubCredentialValidator("admin"),
            new StubAuthTokenIssuer());

        var result = service.SignIn("admin", "secret");

        Assert.NotNull(result);
        Assert.Equal("admin", result.Username);
        Assert.Equal("jwt-token", result.Token.Token);
    }

    private sealed class StubCredentialValidator(string? username) : ICredentialValidator
    {
        public string? Validate(string requestUsername, string password) => username;
    }

    private sealed class StubAuthTokenIssuer : IAuthTokenIssuer
    {
        public IssuedAuthToken IssueToken(string username)
        {
            return new IssuedAuthToken("jwt-token", DateTimeOffset.UtcNow.AddHours(1));
        }
    }
}
