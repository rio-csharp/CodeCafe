namespace CodeCafe.Application.Auth.PersonalAccessTokens.CreatePersonalAccessToken;

// The only response that ever carries the raw token; afterwards only its hash exists.
public sealed record CreatedPersonalAccessTokenDto(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string Token)
{
    // Records are the type most likely to get logged verbatim — keep the token out of ToString().
    public override string ToString() =>
        $"CreatedPersonalAccessTokenDto(Id = {Id}, Name = {Name}, CreatedAtUtc = {CreatedAtUtc:O}, ExpiresAtUtc = {ExpiresAtUtc:O}, Token = ***)";
}
