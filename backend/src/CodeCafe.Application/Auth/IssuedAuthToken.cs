namespace CodeCafe.Application.Auth;

public sealed record IssuedAuthToken(string Token, DateTimeOffset ExpiresAtUtc);
