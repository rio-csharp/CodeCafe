namespace CodeCafe.Application.Auth.Shared;

public sealed record AuthSessionDto(
    AuthUserDto User,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken);
