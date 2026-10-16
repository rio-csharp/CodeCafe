using CodeCafe.Application.Auth.Abstractions;

namespace CodeCafe.Infrastructure.Auth;

// Identity's PasswordHasher is used standalone: adaptive PBKDF2 with a per-password salt and a
// version marker byte, without pulling in the whole Identity stack.
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> _inner = new();

    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string password, string passwordHash)
        => _inner.VerifyHashedPassword(null!, passwordHash, password)
            != Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed;
}
