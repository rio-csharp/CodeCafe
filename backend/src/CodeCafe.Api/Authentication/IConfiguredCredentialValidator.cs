namespace CodeCafe.Api.Authentication;

public interface IConfiguredCredentialValidator
{
    string? Validate(string username, string password);
}
