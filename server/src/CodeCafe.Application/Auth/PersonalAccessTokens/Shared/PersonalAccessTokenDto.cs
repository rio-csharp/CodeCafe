using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Auth.PersonalAccessTokens.Shared;

// Never carries the token hash or the raw token; both stay server-side.
public sealed record PersonalAccessTokenDto(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc)
{
    public static PersonalAccessTokenDto From(PersonalAccessToken token) =>
        new(token.Id, token.Name, token.CreatedAtUtc, token.ExpiresAtUtc, token.RevokedAtUtc);
}
