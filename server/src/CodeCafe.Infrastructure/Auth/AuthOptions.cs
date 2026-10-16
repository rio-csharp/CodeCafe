namespace CodeCafe.Infrastructure.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    // Off unless a deployment turns it on: an instance that is reachable by the public
    // should not accept new accounts because of a missing setting.
    public bool RegistrationEnabled { get; init; }

    public int AccessTokenLifetimeMinutes { get; init; } = 15;

    public int RefreshTokenLifetimeDays { get; init; } = 30;

    public JwtOptions Jwt { get; init; } = new();
}

public sealed class JwtOptions
{
    // Development-only value lives in appsettings.Development.json; production sets
    // Auth__Jwt__SigningKey. HMAC-SHA256 needs at least 32 bytes.
    public string SigningKey { get; init; } = string.Empty;

    public string Issuer { get; init; } = "codecafe";

    public string Audience { get; init; } = "codecafe";
}
