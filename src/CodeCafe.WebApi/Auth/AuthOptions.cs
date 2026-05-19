namespace CodeCafe.WebApi.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public bool RegistrationEnabled { get; set; } = true;
}
