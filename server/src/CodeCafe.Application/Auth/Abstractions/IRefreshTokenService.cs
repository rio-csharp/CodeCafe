namespace CodeCafe.Application.Auth.Abstractions;

// Mutations are staged in the current unit of work; the caller commits via IUnitOfWork.
public interface IRefreshTokenService
{
    Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken);

    // Validates and revokes atomically: refresh tokens are single-use (rotation), so under
    // concurrency exactly one caller wins and the rest get UserId = null. FamilyRevoked is true
    // when a replayed token made the service revoke every active session for that user; the
    // caller must commit that change even though the request itself fails. Runs inside the
    // caller's transaction.
    Task<RefreshTokenConsumption> ConsumeAsync(string token, CancellationToken cancellationToken);

    // Idempotent: unknown or already-revoked tokens are a no-op.
    Task RevokeAsync(string token, CancellationToken cancellationToken);

    // Ends every session for the user at once, e.g. when credentials change.
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed record IssuedRefreshToken(string Value, DateTimeOffset ExpiresAtUtc)
{
    // Records are the type most likely to get logged verbatim — keep the token out of ToString().
    public override string ToString() => $"IssuedRefreshToken(Value = ***, ExpiresAtUtc = {ExpiresAtUtc:O})";
}

public sealed record RefreshTokenConsumption(Guid? UserId, bool FamilyRevoked = false);
