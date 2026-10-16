namespace CodeCafe.Application.Auth.Abstractions;

// The validated contents of an access token. IssuedAtUtc lets the authentication handler
// reject tokens minted before the user's last password change.
public sealed record ValidatedAccessToken(Guid UserId, DateTimeOffset IssuedAtUtc);
