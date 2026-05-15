namespace CodeCafe.Application.Auth;

public interface ICredentialValidator
{
    string? Validate(string username, string password);
}
