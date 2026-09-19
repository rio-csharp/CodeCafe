namespace CodeCafe.Application.Auth.Abstractions;

public interface IAccessTokenService
{
    AccessToken Issue(Guid userId);

    Task<Guid?> ValidateAsync(string token, CancellationToken cancellationToken);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);
