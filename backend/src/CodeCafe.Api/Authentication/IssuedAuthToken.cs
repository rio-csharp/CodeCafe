namespace CodeCafe.Api.Authentication;

public sealed record IssuedAuthToken(string Token, DateTimeOffset ExpiresAtUtc);
