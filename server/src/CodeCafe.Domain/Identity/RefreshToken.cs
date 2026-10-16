using CodeCafe.Domain.Primitives;

namespace CodeCafe.Domain.Identity;

public sealed class RefreshToken : Entity
{
    private RefreshToken()
        : base(Guid.Empty) { }

    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAtUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }

    // Only the SHA-256 hash is stored; the raw token is unrecoverable from the database.
    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public bool IsActive(DateTimeOffset nowUtc) => RevokedAtUtc is null && nowUtc < ExpiresAtUtc;

    public void Revoke(DateTimeOffset revokedAtUtc) => RevokedAtUtc = revokedAtUtc;

    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        return new RefreshToken(Guid.CreateVersion7(), userId, tokenHash, expiresAtUtc);
    }
}
