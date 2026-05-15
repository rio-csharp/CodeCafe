namespace CodeCafe.Application.Auth;

public interface IAuthService
{
    AuthSignInResult? SignIn(string username, string password);
}
