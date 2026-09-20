using CodeCafe.Domain.Identity;
using CodeCafe.Infrastructure.Auth;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace CodeCafe.Infrastructure.Tests;

[Collection(nameof(PostgresCollection))]
public sealed class RefreshTokenServiceTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Issue_Then_Save_PersistsHashOnly()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var service = CreateService(dbContext);
        var userId = await SeedUserAsync(dbContext);

        var token = await service.IssueAsync(userId, CancellationToken.None);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.NotEmpty(token.Value);
        Assert.True(token.ExpiresAtUtc > DateTimeOffset.UtcNow.AddDays(29));

        // The database must not contain anything recoverable from the raw token.
        var stored = Assert.Single(dbContext.RefreshTokens);
        Assert.DoesNotContain(stored.TokenHash, token.Value);
        Assert.Equal(64, stored.TokenHash.Length);
        Assert.Equal(userId, stored.UserId);
    }

    [Fact]
    public async Task Consume_ReturnsUserId_And_RevokesToken_SoItIsSingleUse()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var service = CreateService(dbContext);
        var userId = await SeedUserAsync(dbContext);
        var token = await service.IssueAsync(userId, CancellationToken.None);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(userId, await service.ConsumeAsync(token.Value, CancellationToken.None));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Null(await service.ConsumeAsync(token.Value, CancellationToken.None));
    }

    [Fact]
    public async Task Consume_ReturnsNull_ForUnknownToken()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var service = CreateService(dbContext);

        Assert.Null(await service.ConsumeAsync(Base64UrlEncode(new byte[64]), CancellationToken.None));
    }

    [Fact]
    public async Task Consume_ReturnsNull_ForMalformedToken()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var service = CreateService(dbContext);

        Assert.Null(await service.ConsumeAsync("!!!not-base64url!!!", CancellationToken.None));
    }

    [Fact]
    public async Task Consume_ReturnsNull_ForExpiredToken()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var service = CreateService(dbContext, refreshTokenLifetimeDays: 0);
        var userId = await SeedUserAsync(dbContext);

        var token = await service.IssueAsync(userId, CancellationToken.None);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Null(await service.ConsumeAsync(token.Value, CancellationToken.None));
    }

    [Fact]
    public async Task Revoke_InvalidatesActiveToken()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var service = CreateService(dbContext);
        var userId = await SeedUserAsync(dbContext);
        var token = await service.IssueAsync(userId, CancellationToken.None);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await service.RevokeAsync(token.Value, CancellationToken.None);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Null(await service.ConsumeAsync(token.Value, CancellationToken.None));
    }

    [Fact]
    public async Task Revoke_UnknownToken_IsANoOp()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var service = CreateService(dbContext);

        await service.RevokeAsync(Base64UrlEncode(new byte[64]), CancellationToken.None);
        await service.RevokeAsync("!!!not-base64url!!!", CancellationToken.None);
    }

    [Fact]
    public async Task RevokeAllForUser_RevokesOnlyThatUsersActiveTokens()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var service = CreateService(dbContext);
        var userId = await SeedUserAsync(dbContext);
        var otherUserId = await SeedUserAsync(dbContext, "other-owner@example.com");

        var first = await service.IssueAsync(userId, CancellationToken.None);
        var second = await service.IssueAsync(userId, CancellationToken.None);
        var other = await service.IssueAsync(otherUserId, CancellationToken.None);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await service.RevokeAllForUserAsync(userId, CancellationToken.None);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Null(await service.ConsumeAsync(first.Value, CancellationToken.None));
        Assert.Null(await service.ConsumeAsync(second.Value, CancellationToken.None));
        Assert.Equal(otherUserId, await service.ConsumeAsync(other.Value, CancellationToken.None));
    }

    [Fact]
    public async Task RevokeAllForUser_IsIdempotent()
    {
        await using var dbContext = await fixture.CreateCleanContextAsync();
        var service = CreateService(dbContext);
        var userId = await SeedUserAsync(dbContext);

        await service.RevokeAllForUserAsync(userId, CancellationToken.None);
        await service.RevokeAllForUserAsync(userId, CancellationToken.None);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static RefreshTokenService CreateService(AppDbContext dbContext, int refreshTokenLifetimeDays = 30)
        => new(
            dbContext,
            Options.Create(new AuthOptions { RefreshTokenLifetimeDays = refreshTokenLifetimeDays })
        );

    private static async Task<Guid> SeedUserAsync(AppDbContext dbContext, string email = "token-owner@example.com")
    {
        var user = CodeCafe.Domain.Identity.User.Create(email, email, "Token Owner", "password-hash");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user.Id;
    }

    private static string Base64UrlEncode(byte[] bytes) => System.Buffers.Text.Base64Url.EncodeToString(bytes);
}
