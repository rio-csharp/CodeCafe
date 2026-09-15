namespace CodeCafe.Application.Auth.Models;

public sealed record AuthSessionDto(
    AuthUserDto User,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken);
