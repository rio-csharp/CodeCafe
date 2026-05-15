using CodeCafe.Api.Authentication;
using Microsoft.AspNetCore.Http;

namespace CodeCafe.UnitTests.Api.Authentication;

public sealed class AuthSessionServiceTests
{
    [Fact]
    public void SignIn_returns_null_and_does_not_write_cookie_when_credentials_are_invalid()
    {
        var cookieManager = new StubAuthCookieManager();
        var service = new AuthSessionService(
            new StubCredentialValidator(null),
            new StubJwtTokenIssuer(),
            cookieManager);

        var username = service.SignIn(new DefaultHttpContext(), "admin", "bad");

        Assert.Null(username);
        Assert.False(cookieManager.AppendWasCalled);
    }

    [Fact]
    public void SignIn_issues_token_and_writes_cookie_when_credentials_are_valid()
    {
        var cookieManager = new StubAuthCookieManager();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        var service = new AuthSessionService(
            new StubCredentialValidator("admin"),
            new StubJwtTokenIssuer(),
            cookieManager);

        var username = service.SignIn(httpContext, "admin", "secret");

        Assert.Equal("admin", username);
        Assert.True(cookieManager.AppendWasCalled);
        Assert.True(cookieManager.LastUseSecureCookies);
        Assert.Equal("jwt-token", cookieManager.LastToken?.Token);
    }

    [Fact]
    public void SignOut_deletes_cookie()
    {
        var cookieManager = new StubAuthCookieManager();
        var service = new AuthSessionService(
            new StubCredentialValidator("admin"),
            new StubJwtTokenIssuer(),
            cookieManager);

        service.SignOut(new DefaultHttpContext());

        Assert.True(cookieManager.DeleteWasCalled);
    }

    private sealed class StubCredentialValidator(string? username) : IConfiguredCredentialValidator
    {
        public string? Validate(string requestUsername, string password) => username;
    }

    private sealed class StubJwtTokenIssuer : IJwtTokenIssuer
    {
        public IssuedAuthToken IssueToken(string username)
        {
            return new IssuedAuthToken("jwt-token", DateTimeOffset.UtcNow.AddHours(1));
        }
    }

    private sealed class StubAuthCookieManager : IAuthCookieManager
    {
        public bool AppendWasCalled { get; private set; }

        public bool DeleteWasCalled { get; private set; }

        public bool LastUseSecureCookies { get; private set; }

        public IssuedAuthToken? LastToken { get; private set; }

        public void Append(HttpResponse response, bool useSecureCookies, IssuedAuthToken token)
        {
            AppendWasCalled = true;
            LastUseSecureCookies = useSecureCookies;
            LastToken = token;
        }

        public void Delete(HttpResponse response, bool useSecureCookies)
        {
            DeleteWasCalled = true;
            LastUseSecureCookies = useSecureCookies;
        }
    }
}
