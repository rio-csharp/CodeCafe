using CodeCafe.Domain.Identity;
using CodeCafe.Infrastructure.Auth;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace CodeCafe.Infrastructure.Tests;

[Collection(nameof(PostgresCollection))]
public sealed class RefreshTokenServiceTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Issue_Then_Save_PersistsHashOnly_And_ValidateAsync_RoundTrips()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var service = CreateService(dbContext);
        var userId = await SeedUserAsync(dbContext);

        var token = await service.IssueAsync(userId, CancellationToken.None);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.NotEmpty(token.Value);
        Assert.True(token.ExpiresAtUtc > DateTimeOffset.UtcNow.AddDays(29));
        Assert.Equal(userId, await service.ValidateAsync(token.Value, CancellationToken.None));

        // The database must not contain anything recoverable from the raw token.
        var stored = Assert.Single(dbContext.RefreshTokens);
        Assert.DoesNotContain(stored.TokenHash, token.Value);
        Assert.Equal(64, stored.TokenHash.Length);
        Assert.Equal(userId, stored.UserId);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNull_ForUnknownToken()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var service = CreateService(dbContext);

        Assert.Null(await service.ValidateAsync(Base64UrlEncode(new byte[64]), CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNull_ForMalformedToken()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var service = CreateService(dbContext);

        Assert.Null(await service.ValidateAsync("!!!not-base64url!!!", CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNull_ForExpiredToken()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var service = CreateService(dbContext, refreshTokenLifetimeDays: 0);
        var userId = await SeedUserAsync(dbContext);

        var token = await service.IssueAsync(userId, CancellationToken.None);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Null(await service.ValidateAsync(token.Value, CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNull_ForRevokedToken()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var service = CreateService(dbContext);
        var userId = await SeedUserAsync(dbContext);
        var token = await service.IssueAsync(userId, CancellationToken.None);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var stored = Assert.Single(dbContext.RefreshTokens);
        stored.Revoke(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Null(await service.ValidateAsync(token.Value, CancellationToken.None));
    }

    private static RefreshTokenService CreateService(AppDbContext dbContext, int refreshTokenLifetimeDays = 30)
        => new(
            dbContext,
            Options.Create(new AuthOptions { RefreshTokenLifetimeDays = refreshTokenLifetimeDays })
        );

    private static async Task<Guid> SeedUserAsync(AppDbContext dbContext)
    {
        var user = CodeCafe.Domain.Identity.User.Create(
            "token-owner@example.com",
            "token-owner@example.com",
            "Token Owner",
            "password-hash"
        );
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user.Id;
    }

    private static string Base64UrlEncode(byte[] bytes) => System.Buffers.Text.Base64Url.EncodeToString(bytes);
}
