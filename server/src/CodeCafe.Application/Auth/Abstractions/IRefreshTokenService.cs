namespace CodeCafe.Application.Auth.Abstractions;

// Mutations are staged in the current unit of work; the caller commits via IUnitOfWork.
public interface IRefreshTokenService
{
    Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken);

    // Validates and revokes atomically: refresh tokens are single-use (rotation), so under
    // concurrency exactly one caller wins and the rest get null. Runs inside the caller's
    // transaction.
    Task<Guid?> ConsumeAsync(string token, CancellationToken cancellationToken);

    // Idempotent: unknown or already-revoked tokens are a no-op.
    Task RevokeAsync(string token, CancellationToken cancellationToken);

    // Ends every session for the user at once, e.g. when credentials change.
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed record IssuedRefreshToken(string Value, DateTimeOffset ExpiresAtUtc);
