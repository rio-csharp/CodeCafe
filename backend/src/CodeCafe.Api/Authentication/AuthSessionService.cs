namespace CodeCafe.Api.Authentication;

public sealed class AuthSessionService(
    IConfiguredCredentialValidator credentialValidator,
    IJwtTokenIssuer jwtTokenIssuer,
    IAuthCookieManager authCookieManager) : IAuthSessionService
{
    public string? SignIn(HttpContext httpContext, string username, string password)
    {
        var authenticatedUsername = credentialValidator.Validate(username, password);

        if (authenticatedUsername is null)
        {
            return null;
        }

        var token = jwtTokenIssuer.IssueToken(authenticatedUsername);
        authCookieManager.Append(httpContext.Response, httpContext.Request.IsHttps, token);

        return authenticatedUsername;
    }

    public void SignOut(HttpContext httpContext)
    {
        authCookieManager.Delete(httpContext.Response, httpContext.Request.IsHttps);
    }
}
