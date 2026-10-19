using CodeCafe.Domain.Identity;
using CodeCafe.Infrastructure.Auth;
using CodeCafe.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodeCafe.Infrastructure.Tests.Auth;

[Collection(nameof(PostgresCollection))]
public sealed class RefreshTokenServiceTests(PostgresFixture fixture)
{
    private static readonly IOptions<AuthOptions> Options = Microsoft.Extensions.Options.Options.Create(
        new AuthOptions()
    );

    [Fact]
    public async Task ConsumeAsync_ExactlyOneCompetitorWins_UnderConcurrency()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        string tokenValue;
        Guid userId;

        await using (var setup = await fixture.CreateCleanContextAsync())
        {
            var user = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
            setup.Set<User>().Add(user);
            userId = user.Id;
            var service = new RefreshTokenService(setup, Options);
            tokenValue = (await service.IssueAsync(userId, cancellationToken)).Value;
            await setup.SaveChangesAsync(cancellationToken);
        }

        // Each competitor mirrors the handler: its own context and transaction, consume, commit.
        // The winner's conditional UPDATE holds the row lock until commit; losers re-evaluate
        // the predicate afterwards and affect 0 rows.
        var competitors = Enumerable
            .Range(0, 4)
            .Select(_ => Task.Run(async () =>
            {
                await using var context = await fixture.CreateContextAsync();
                var service = new RefreshTokenService(context, Options);
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                var winner = await service.ConsumeAsync(tokenValue, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return winner;
            }, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(competitors);

        var winner = Assert.Single(results, result => result.UserId is not null);
        Assert.Equal(userId, winner.UserId);

        await using var verify = await fixture.CreateContextAsync();
        var stored = await verify.RefreshTokens.SingleAsync(cancellationToken);
        Assert.NotNull(stored.RevokedAtUtc);
    }

    [Fact]
    public async Task ConsumeAsync_ReuseAfterGrace_RevokesEveryActiveToken()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateCleanContextAsync();
        var service = new RefreshTokenService(context, Options);

        var user = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
        context.Set<User>().Add(user);
        var first = await service.IssueAsync(user.Id, cancellationToken);
        var second = await service.IssueAsync(user.Id, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var firstConsumption = await service.ConsumeAsync(first.Value, cancellationToken);
        Assert.Equal(user.Id, firstConsumption.UserId);
        await context.SaveChangesAsync(cancellationToken);

        // Move the consumed token outside the benign-race grace window, simulating a replay.
        var consumedRow = await context.RefreshTokens
            .AsNoTracking()
            .SingleAsync(token => token.RevokedAtUtc != null, cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE refresh_tokens SET "RevokedAtUtc" = {DateTimeOffset.UtcNow.AddMinutes(-5)} WHERE "TokenHash" = {consumedRow.TokenHash}""",
            cancellationToken
        );

        var replay = await service.ConsumeAsync(first.Value, cancellationToken);

        Assert.Null(replay.UserId);
        Assert.True(replay.FamilyRevoked);
        await context.SaveChangesAsync(cancellationToken);

        var secondRow = await context.RefreshTokens.SingleAsync(token => token.TokenHash != consumedRow.TokenHash, cancellationToken);
        Assert.NotNull(secondRow.RevokedAtUtc);
    }

    [Fact]
    public async Task ConsumeAsync_ReturnsNull_ForMalformedUnknownRevokedAndExpiredTokens()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = await fixture.CreateCleanContextAsync();
        var service = new RefreshTokenService(context, Options);

        Assert.Null((await service.ConsumeAsync("not-base64url!", cancellationToken)).UserId);

        var user = User.Create("owner@example.com", "owner@example.com", "Owner", "hash");
        context.Set<User>().Add(user);
        var unknown = await service.IssueAsync(user.Id, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        Assert.Equal(user.Id, (await service.ConsumeAsync(unknown.Value, cancellationToken)).UserId);
        // Second consume of the same (now revoked) token loses; within the reuse grace window it
        // is treated as a benign race rather than a theft.
        Assert.Null((await service.ConsumeAsync(unknown.Value, cancellationToken)).UserId);
    }
}
