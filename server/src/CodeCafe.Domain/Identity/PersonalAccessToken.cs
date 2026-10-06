using CodeCafe.Domain.Primitives;

namespace CodeCafe.Domain.Identity;

public sealed class PersonalAccessToken : Entity
{
    public const int MaxNameLength = 50;

    private PersonalAccessToken()
        : base(Guid.Empty) { }

    private PersonalAccessToken(Guid id, Guid userId, string name, string tokenHash, DateTimeOffset expiresAtUtc)
        : base(id)
    {
        UserId = userId;
        Name = name;
        TokenHash = tokenHash;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    // Only the SHA-256 hash is stored; the raw token is unrecoverable from the database.
    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public bool IsActive(DateTimeOffset nowUtc) => RevokedAtUtc is null && nowUtc < ExpiresAtUtc;

    public void Revoke(DateTimeOffset revokedAtUtc) => RevokedAtUtc = revokedAtUtc;

    public static PersonalAccessToken Create(Guid userId, string name, string tokenHash, DateTimeOffset expiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        return new PersonalAccessToken(Guid.CreateVersion7(), userId, name, tokenHash, expiresAtUtc);
    }
}
