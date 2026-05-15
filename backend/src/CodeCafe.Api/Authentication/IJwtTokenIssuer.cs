namespace CodeCafe.Api.Authentication;

public interface IJwtTokenIssuer
{
    IssuedAuthToken IssueToken(string username);
}
