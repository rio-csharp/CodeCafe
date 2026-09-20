using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.Logout;
using CodeCafe.Application.Common.Abstractions;

namespace CodeCafe.Application.Tests;

public sealed class LogoutCommandHandlerTests
{
    [Fact]
    public async Task Handle_RevokesTokenAndCommits()
    {
        var refreshTokens = new StubRefreshTokenService();
        var unitOfWork = new StubUnitOfWork();
        var handler = new LogoutCommandHandler(unitOfWork, refreshTokens);

        var result = await handler.Handle(new LogoutCommand("some-token"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("some-token", Assert.Single(refreshTokens.RevokedTokens));
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenTokenIsUnknown()
    {
        var refreshTokens = new StubRefreshTokenService();
        var unitOfWork = new StubUnitOfWork();
        var handler = new LogoutCommandHandler(unitOfWork, refreshTokens);

        var result = await handler.Handle(new LogoutCommand("no-such-token"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubRefreshTokenService : IRefreshTokenService
    {
        public List<string> RevokedTokens { get; } = [];

        public Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(new IssuedRefreshToken("refresh-token", DateTimeOffset.UtcNow.AddDays(30)));

        public Task<Guid?> ConsumeAsync(string token, CancellationToken cancellationToken)
            => Task.FromResult<Guid?>(null);

        public Task RevokeAsync(string token, CancellationToken cancellationToken)
        {
            RevokedTokens.Add(token);
            return Task.CompletedTask;
        }

        public Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
