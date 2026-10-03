using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.Register;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Tests.Auth;

public sealed class RegisterCommandHandlerTests
{
    private static readonly RegisterCommand Command = new("  Yao@Example.COM ", "Password123!", "  Yao  ");

    [Fact]
    public async Task Handle_PersistsNormalizedUserAndReturnsSession()
    {
        var users = new StubUserRepository();
        var handler = CreateHandler(users, out var accessTokens, out var refreshTokens, out var unitOfWork);

        var result = await handler.Handle(Command, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var savedUser = Assert.Single(users);
        Assert.Equal("yao@example.com", savedUser.NormalizedEmail);
        Assert.Equal("Yao@Example.COM", savedUser.Email);
        Assert.Equal("Yao", savedUser.DisplayName);
        Assert.NotEqual(Command.Password, savedUser.PasswordHash);
        Assert.NotEmpty(savedUser.PasswordHash);

        var session = result.Value!;
        Assert.Equal(savedUser.Id, session.User.Id);
        Assert.Equal(savedUser.Email, session.User.Email);
        Assert.Equal(savedUser.DisplayName, session.User.DisplayName);
        Assert.Equal("access-token", session.AccessToken);
        Assert.True(session.AccessTokenExpiresAtUtc > DateTimeOffset.UtcNow);
        Assert.Equal("refresh-token", session.RefreshToken);
        Assert.Equal(savedUser.Id, accessTokens.IssuedFor);
        Assert.Equal(savedUser.Id, refreshTokens.IssuedFor);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ReturnsConflict_WhenNormalizedEmailAlreadyRegistered()
    {
        var existing = User.Create("first@example.com", "first@example.com", "First", "hash");
        var users = new StubUserRepository { existing };
        var handler = CreateHandler(users, out _, out _, out var unitOfWork);

        var result = await handler.Handle(
            new RegisterCommand("FIRST@example.com", "Password123!", "Second"),
            CancellationToken.None
        );

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.EmailAlreadyRegistered, result.Error);
        Assert.Single(users);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static RegisterCommandHandler CreateHandler(
        StubUserRepository users,
        out StubAccessTokenService accessTokens,
        out StubRefreshTokenService refreshTokens,
        out StubUnitOfWork unitOfWork
    )
    {
        accessTokens = new StubAccessTokenService();
        refreshTokens = new StubRefreshTokenService();
        unitOfWork = new StubUnitOfWork();
        return new RegisterCommandHandler(users, unitOfWork, new StubPasswordHasher(), accessTokens, refreshTokens);
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

    private sealed class StubPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string passwordHash) => passwordHash == $"hashed:{password}";
    }

    private sealed class StubAccessTokenService : IAccessTokenService
    {
        public Guid IssuedFor { get; private set; }

        public AccessToken Issue(Guid userId)
        {
            IssuedFor = userId;
            return new AccessToken("access-token", DateTimeOffset.UtcNow.AddMinutes(15));
        }

        public Task<ValidatedAccessToken?> ValidateAsync(string token, CancellationToken cancellationToken) => Task.FromResult<ValidatedAccessToken?>(null);
    }

    private sealed class StubRefreshTokenService : IRefreshTokenService
    {
        public Guid? IssuedFor { get; private set; }

        public Task<IssuedRefreshToken> IssueAsync(Guid userId, CancellationToken cancellationToken)
        {
            IssuedFor = userId;
            return Task.FromResult(new IssuedRefreshToken("refresh-token", DateTimeOffset.UtcNow.AddDays(30)));
        }

        public Task<Guid?> ConsumeAsync(string token, CancellationToken cancellationToken)
            => Task.FromResult<Guid?>(null);

        public Task RevokeAsync(string token, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
