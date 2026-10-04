using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Auth.Shared;

public sealed record AuthSessionDto(
    AuthUserDto User,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken)
{
    // Register, login and refresh all end by issuing this same session shape.
    public static AuthSessionDto From(User user, AccessToken accessToken, IssuedRefreshToken refreshToken) =>
        new(
            new AuthUserDto(user.Id, user.Email, user.DisplayName),
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            refreshToken.Value
        );

    // Records are the type most likely to get logged verbatim — keep the tokens out of ToString().
    public override string ToString() =>
        $"AuthSessionDto(User = {User}, AccessToken = ***, AccessTokenExpiresAtUtc = {AccessTokenExpiresAtUtc:O}, RefreshToken = ***)";
}
