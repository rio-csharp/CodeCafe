namespace CodeCafe.Api.Authentication;

public interface IAuthSessionService
{
    string? SignIn(HttpContext httpContext, string username, string password);

    void SignOut(HttpContext httpContext);
}
