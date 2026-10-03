using CodeCafe.Domain.Primitives;

namespace CodeCafe.Domain.Identity;

public sealed class User : Entity
{
    public const int MaxEmailLength = 256;
    public const int MaxDisplayNameLength = 40;

    private User()
        : base(Guid.Empty) { }

    private User(Guid id, string email, string normalizedEmail, string displayName, string passwordHash)
        : base(id)
    {
        Email = email;
        NormalizedEmail = normalizedEmail;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        PasswordChangedAtUtc = CreatedAtUtc;
    }

    // Original casing for display; lookups and uniqueness run on NormalizedEmail instead.
    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    // Access tokens are stateless, so revoking them on password change means comparing their
    // iat against this timestamp in the authentication handler.
    public DateTimeOffset PasswordChangedAtUtc { get; private set; }

    // JWT iat is second-precision; compare at that granularity so a token minted in the same
    // second as the change (but after it) is not falsely rejected.
    public bool IsAccessTokenCurrent(DateTimeOffset issuedAtUtc)
        => issuedAtUtc.ToUnixTimeSeconds() >= PasswordChangedAtUtc.ToUnixTimeSeconds();

    public void ChangeDisplayName(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        DisplayName = displayName;
    }

    // Takes a ready hash so callers can never pass a plaintext password by mistake.
    public void ChangePasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        PasswordHash = passwordHash;
        PasswordChangedAtUtc = DateTimeOffset.UtcNow;
        Raise(new PasswordChangedEvent(Id));
    }

    public static User Create(string email, string normalizedEmail, string displayName, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User(Guid.CreateVersion7(), email, normalizedEmail, displayName, passwordHash);
    }
}
