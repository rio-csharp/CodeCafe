using CodeCafe.Application.Auth;

namespace CodeCafe.Api.Authentication;

public interface IAuthCookieManager
{
    void Append(HttpResponse response, bool useSecureCookies, IssuedAuthToken token);

    void Delete(HttpResponse response, bool useSecureCookies);
}
