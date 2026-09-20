using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.ChangePassword;
using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Tests;

public sealed class RevokeSessionsOnPasswordChangedTests
{
    [Fact]
    public async Task Handle_RevokesAllSessionsForTheUser()
    {
        var refreshTokens = new StubRefreshTokenService();
        var handler = new RevokeSessionsOnPasswordChanged(refreshTokens);
        var userId = Guid.CreateVersion7();

        await handler.Handle(new PasswordChangedEvent(userId), CancellationToken.None);

        Assert.Equal(userId, refreshTokens.RevokedAllFor);
    }

    private sealed class StubRefreshTokenService : IRefreshTokenService
    {
        public Guid? RevokedAllFor { get; private set; }

        public Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(new IssuedRefreshToken("refresh-token", DateTimeOffset.UtcNow.AddDays(30)));

        public Task<Guid?> ConsumeAsync(string token, CancellationToken cancellationToken)
            => Task.FromResult<Guid?>(null);

        public Task RevokeAsync(string token, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            RevokedAllFor = userId;
            return Task.CompletedTask;
        }
    }
}
