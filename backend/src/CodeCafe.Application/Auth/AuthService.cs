namespace CodeCafe.Application.Auth;

public sealed class AuthService(
    ICredentialValidator credentialValidator,
    IAuthTokenIssuer tokenIssuer) : IAuthService
{
    public AuthSignInResult? SignIn(string username, string password)
    {
        var authenticatedUsername = credentialValidator.Validate(username, password);

        if (authenticatedUsername is null)
        {
            return null;
        }

        return new AuthSignInResult(authenticatedUsername, tokenIssuer.IssueToken(authenticatedUsername));
    }
}
