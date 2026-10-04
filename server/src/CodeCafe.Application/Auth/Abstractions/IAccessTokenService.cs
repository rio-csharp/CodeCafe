namespace CodeCafe.Application.Auth.Abstractions;

public interface IAccessTokenService
{
    AccessToken Issue(Guid userId);

    Task<ValidatedAccessToken?> ValidateAsync(string token, CancellationToken cancellationToken);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc)
{
    // Records are the type most likely to get logged verbatim — keep the token out of ToString().
    public override string ToString() => $"AccessToken(Value = ***, ExpiresAtUtc = {ExpiresAtUtc:O})";
}
