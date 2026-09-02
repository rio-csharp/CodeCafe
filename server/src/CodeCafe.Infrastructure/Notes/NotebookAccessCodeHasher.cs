using CodeCafe.Application.Notes;
using Microsoft.AspNetCore.Identity;

namespace CodeCafe.Infrastructure.Notes;

public sealed class NotebookAccessCodeHasher : INotebookAccessCodeHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string accessCode) => _hasher.HashPassword(new object(), accessCode);

    public bool Verify(string accessCodeHash, string providedCode)
    {
        return _hasher.VerifyHashedPassword(new object(), accessCodeHash, providedCode)
            is PasswordVerificationResult.Success;
    }
}
