using CodeCafe.Api.Authentication;
using CodeCafe.Application.Auth;
using Microsoft.AspNetCore.Http;

namespace CodeCafe.UnitTests.Api.Authentication;

public sealed class AuthCookieManagerTests
{
    [Fact]
    public void Append_sets_expected_auth_cookie()
    {
        var response = new DefaultHttpContext().Response;
        var manager = new AuthCookieManager();
        var token = new IssuedAuthToken("jwt-token", new DateTimeOffset(2026, 5, 18, 12, 0, 0, TimeSpan.Zero));

        manager.Append(response, useSecureCookies: true, token);

        var setCookieHeader = Assert.Single(response.Headers.SetCookie);
        Assert.Contains("codecafe.auth=jwt-token", setCookieHeader);
        Assert.Contains("secure", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", setCookieHeader, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Delete_issues_cookie_removal_header()
    {
        var response = new DefaultHttpContext().Response;
        var manager = new AuthCookieManager();

        manager.Delete(response, useSecureCookies: false);

        var setCookieHeader = Assert.Single(response.Headers.SetCookie);
        Assert.Contains("codecafe.auth=", setCookieHeader);
        Assert.Contains("expires=", setCookieHeader, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secure", setCookieHeader, StringComparison.OrdinalIgnoreCase);
    }
}
