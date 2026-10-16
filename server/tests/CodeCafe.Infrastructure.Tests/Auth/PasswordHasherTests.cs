using CodeCafe.Infrastructure.Auth;

namespace CodeCafe.Infrastructure.Tests.Auth;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ReturnsVerifiableHash()
    {
        var hash = _hasher.Hash("Password123!");

        Assert.NotEqual("Password123!", hash);
        Assert.True(_hasher.Verify("Password123!", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var hash = _hasher.Hash("Password123!");

        Assert.False(_hasher.Verify("Different123!", hash));
    }

    [Fact]
    public void Hash_Salts_EachPassword()
    {
        var first = _hasher.Hash("Password123!");
        var second = _hasher.Hash("Password123!");

        Assert.NotEqual(first, second);
        Assert.True(_hasher.Verify("Password123!", first));
        Assert.True(_hasher.Verify("Password123!", second));
    }
}
