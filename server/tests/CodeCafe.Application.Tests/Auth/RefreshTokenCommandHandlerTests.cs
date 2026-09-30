using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.RefreshToken;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Tests.Auth;

public sealed class RefreshTokenCommandHandlerTests
{
    private static readonly User ExistingUser = User.Create(
        "Yao@Example.COM",
        "yao@example.com",
        "Yao",
        "hashed:Password123!"
    );

    [Fact]
    public async Task Handle_RotatesTokenAndReturnsNewSession_WhenTokenIsValid()
    {
        var users = new StubUserRepository { ExistingUser };
        var handler = CreateHandler(users, out var accessTokens, out var refreshTokens, out var unitOfWork);
        refreshTokens.UserIdForToken["old-token"] = ExistingUser.Id;

        var result = await handler.Handle(new RefreshTokenCommand("old-token"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("old-token", Assert.Single(refreshTokens.ConsumedTokens));

        var session = result.Value!;
        Assert.Equal(ExistingUser.Id, session.User.Id);
        Assert.Equal(ExistingUser.Email, session.User.Email);
        Assert.Equal("access-token", session.AccessToken);
        Assert.Equal("refresh-token", session.RefreshToken);
        Assert.Equal(ExistingUser.Id, accessTokens.IssuedFor);
        Assert.Equal(ExistingUser.Id, refreshTokens.IssuedFor);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ReturnsInvalidRefreshToken_WhenTokenIsRejected()
    {
        var handler = CreateHandler(new StubUserRepository(), out var accessTokens, out var refreshTokens, out var unitOfWork);

        var result = await handler.Handle(new RefreshTokenCommand("bad-token"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.InvalidRefreshToken, result.Error);
        Assert.Null(accessTokens.IssuedFor);
        Assert.Null(refreshTokens.IssuedFor);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ReturnsInvalidRefreshToken_WhenUserNoLongerExists()
    {
        var handler = CreateHandler(new StubUserRepository(), out _, out var refreshTokens, out var unitOfWork);
        refreshTokens.UserIdForToken["orphaned-token"] = Guid.NewGuid();

        var result = await handler.Handle(new RefreshTokenCommand("orphaned-token"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.InvalidRefreshToken, result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static RefreshTokenCommandHandler CreateHandler(
        StubUserRepository users,
        out StubAccessTokenService accessTokens,
        out StubRefreshTokenService refreshTokens,
        out StubUnitOfWork unitOfWork
    )
    {
        accessTokens = new StubAccessTokenService();
        refreshTokens = new StubRefreshTokenService();
        unitOfWork = new StubUnitOfWork();
        return new RefreshTokenCommandHandler(users, unitOfWork, accessTokens, refreshTokens);
    }

    private sealed class StubUserRepository : List<User>, IUserRepository
    {
        public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
            => Task.FromResult(this.FirstOrDefault(user => user.NormalizedEmail == normalizedEmail));

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(this.FirstOrDefault(user => user.Id == id));

        public Task<IReadOnlyList<User>> FindByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<User>>(this.Where(user => ids.Contains(user.Id)).ToList());

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }

        public Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken)
            => Task.FromResult<ITransaction>(StubTransaction.Instance);
    }

    private sealed class StubAccessTokenService : IAccessTokenService
    {
        public Guid? IssuedFor { get; private set; }

        public AccessToken Issue(Guid userId)
        {
            IssuedFor = userId;
            return new AccessToken("access-token", DateTimeOffset.UtcNow.AddMinutes(15));
        }

        public Task<Guid?> ValidateAsync(string token, CancellationToken cancellationToken) => Task.FromResult<Guid?>(null);
    }

    private sealed class StubRefreshTokenService : IRefreshTokenService
    {
        public Dictionary<string, Guid> UserIdForToken { get; } = new();

        public List<string> ConsumedTokens { get; } = [];

        public Guid? IssuedFor { get; private set; }

        public Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken)
        {
            IssuedFor = userId;
            return Task.FromResult(new IssuedRefreshToken("refresh-token", DateTimeOffset.UtcNow.AddDays(30)));
        }

        public Task<Guid?> ConsumeAsync(string token, CancellationToken cancellationToken)
        {
            ConsumedTokens.Add(token);
            return Task.FromResult(UserIdForToken.TryGetValue(token, out var userId) ? userId : (Guid?)null);
        }

        public Task RevokeAsync(string token, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
