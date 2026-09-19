namespace CodeCafe.Application.Auth.Abstractions;

// Issuing stages the token in the current unit of work (the caller commits via IUnitOfWork);
// validating looks it up by hash.
public interface IRefreshTokenService
{
    Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken);

    Task<Guid?> ValidateAsync(string token, CancellationToken cancellationToken);
}

public sealed record IssuedRefreshToken(string Value, DateTimeOffset ExpiresAtUtc);
