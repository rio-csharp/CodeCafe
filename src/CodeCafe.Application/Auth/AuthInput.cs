namespace CodeCafe.Application.Auth;

public static class AuthInput
{
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static string NormalizeDisplayName(string displayName) => displayName.Trim();
}
