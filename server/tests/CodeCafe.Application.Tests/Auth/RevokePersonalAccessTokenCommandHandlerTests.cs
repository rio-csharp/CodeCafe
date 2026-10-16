using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.PersonalAccessTokens.RevokePersonalAccessToken;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Security;
using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Tests.Auth;

public sealed class RevokePersonalAccessTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_RevokesOwnToken()
    {
        var userId = Guid.CreateVersion7();
        var token = PersonalAccessToken.Create(userId, "mcp", "hash-a", DateTimeOffset.UtcNow.AddDays(90));
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(userId, new StubPersonalAccessTokenRepository { token }, unitOfWork);

        var result = await handler.Handle(new RevokePersonalAccessTokenCommand(token.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(token.RevokedAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_OtherUsersToken_ReturnsSameNotFound_AsNonexistent()
    {
        var userId = Guid.CreateVersion7();
        var foreign = PersonalAccessToken.Create(Guid.CreateVersion7(), "not-mine", "hash-b", DateTimeOffset.UtcNow.AddDays(90));
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(userId, new StubPersonalAccessTokenRepository { foreign }, unitOfWork);

        var foreignResult = await handler.Handle(new RevokePersonalAccessTokenCommand(foreign.Id), CancellationToken.None);
        var nonexistentResult = await handler.Handle(new RevokePersonalAccessTokenCommand(Guid.CreateVersion7()), CancellationToken.None);

        Assert.False(foreignResult.IsSuccess);
        Assert.Equal(AuthErrors.PersonalAccessTokenNotFound, foreignResult.Error);
        Assert.False(nonexistentResult.IsSuccess);
        Assert.Equal(foreignResult.Error, nonexistentResult.Error);
        Assert.Null(foreign.RevokedAtUtc);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_AlreadyRevoked_IsIdempotentSuccess()
    {
        var userId = Guid.CreateVersion7();
        var token = PersonalAccessToken.Create(userId, "mcp", "hash-a", DateTimeOffset.UtcNow.AddDays(90));
        token.Revoke(DateTimeOffset.UtcNow.AddDays(-1));
        var originalRevokedAtUtc = token.RevokedAtUtc;
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(userId, new StubPersonalAccessTokenRepository { token }, unitOfWork);

        var result = await handler.Handle(new RevokePersonalAccessTokenCommand(token.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // No re-revocation, no save: the timestamp stays the original one.
        Assert.Equal(originalRevokedAtUtc, token.RevokedAtUtc);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static RevokePersonalAccessTokenCommandHandler CreateHandler(
        Guid userId,
        StubPersonalAccessTokenRepository tokens,
        StubUnitOfWork unitOfWork
    ) => new(new StubCurrentUserAccessor(new CurrentUser(userId)), tokens, unitOfWork);

    private sealed class StubCurrentUserAccessor(CurrentUser? user) : ICurrentUserAccessor
    {
        public CurrentUser? User => user;
    }

    private sealed class StubPersonalAccessTokenRepository : List<PersonalAccessToken>, IPersonalAccessTokenRepository
    {
        public Task AddAsync(PersonalAccessToken token, CancellationToken cancellationToken)
        {
            Add(token);
            return Task.CompletedTask;
        }

        public Task<PersonalAccessToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken)
            => Task.FromResult(this.FirstOrDefault(token => token.TokenHash == tokenHash));

        public Task<IReadOnlyList<PersonalAccessToken>> ListByUserAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PersonalAccessToken>>(this.Where(token => token.UserId == userId).ToList());
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
}
