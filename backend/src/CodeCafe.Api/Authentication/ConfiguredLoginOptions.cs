namespace CodeCafe.Api.Authentication;

public sealed class ConfiguredLoginOptions
{
    public const string SectionName = "Authentication";

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string JwtSigningKey { get; init; } = string.Empty;

    public int JwtTokenLifetimeMinutes { get; init; } = 60 * 24 * 3;
}
