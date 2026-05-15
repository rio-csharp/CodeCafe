namespace CodeCafe.Application.Auth;

public interface IAuthTokenIssuer
{
    IssuedAuthToken IssueToken(string username);
}
