using CodeCafe.Api.Configuration;

namespace CodeCafe.Api.Authentication;

public sealed class AuthCookieManager : IAuthCookieManager
{
    public void Append(HttpResponse response, bool useSecureCookies, IssuedAuthToken token)
    {
        response.Cookies.Append(
            ApiCookieNames.AuthCookieName,
            token.Token,
            CreateCookieOptions(useSecureCookies, token.ExpiresAtUtc));
    }

    public void Delete(HttpResponse response, bool useSecureCookies)
    {
        response.Cookies.Delete(
            ApiCookieNames.AuthCookieName,
            CreateCookieOptions(useSecureCookies));
    }

    private static CookieOptions CreateCookieOptions(bool useSecureCookies, DateTimeOffset? expiresAtUtc = null)
    {
        return new CookieOptions
        {
            Expires = expiresAtUtc,
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = useSecureCookies,
        };
    }
}
