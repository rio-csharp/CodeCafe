using CodeCafe.Domain.Identity;

namespace CodeCafe.Domain.Tests.Identity;

public sealed class UserTests
{
    [Fact]
    public void ChangePasswordHash_AdvancesPasswordChangedAtUtc()
    {
        var user = User.Create("a@example.com", "a@example.com", "A", "hash");
        Assert.Equal(user.CreatedAtUtc, user.PasswordChangedAtUtc);

        user.ChangePasswordHash("new-hash");

        Assert.True(user.PasswordChangedAtUtc >= user.CreatedAtUtc);
    }

    [Fact]
    public void IsAccessTokenCurrent_RejectsTokensIssuedBeforeTheChange()
    {
        var user = User.Create("a@example.com", "a@example.com", "A", "hash");
        user.ChangePasswordHash("new-hash");

        var before = DateTimeOffset.UtcNow.AddMinutes(-5);
        var after = DateTimeOffset.UtcNow.AddSeconds(5);

        Assert.False(user.IsAccessTokenCurrent(before));
        Assert.True(user.IsAccessTokenCurrent(after));
    }

    [Fact]
    public void IsAccessTokenCurrent_ComparesAtSecondPrecision()
    {
        var user = User.Create("a@example.com", "a@example.com", "A", "hash");
        user.ChangePasswordHash("new-hash");

        // Same second as the change, sub-second earlier: still current, because JWT iat
        // cannot express sub-second ordering.
        var sameSecond = DateTimeOffset.FromUnixTimeSeconds(user.PasswordChangedAtUtc.ToUnixTimeSeconds());

        Assert.True(user.IsAccessTokenCurrent(sameSecond));
    }
}
