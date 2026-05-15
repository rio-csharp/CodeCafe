namespace CodeCafe.Application.Auth;

public sealed record AuthTokenValidationConfiguration(string SigningKey, TimeSpan ClockSkew);
