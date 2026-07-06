namespace CodeCafe.Api.Configuration;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public bool RegistrationEnabled { get; set; } = true;
}
