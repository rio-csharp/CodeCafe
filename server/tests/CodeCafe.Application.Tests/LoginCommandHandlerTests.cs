using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.Login;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Tests;

public sealed class LoginCommandHandlerTests
{
    private static readonly User ExistingUser = User.Create(
        "Yao@Example.COM",
        "yao@example.com",
        "Yao",
        "hashed:Password123!"
    );

    [Fact]
    public async Task Handle_ReturnsSession_WhenCredentialsAreValid()
    {
        var users = new StubUserRepository { ExistingUser };
        var handler = CreateHandler(users, out var accessTokens, out var refreshTokens, out var unitOfWork);

        var result = await handler.Handle(
            new LoginCommand("  YAO@example.com ", "Password123!"),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);

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
    public async Task Handle_ReturnsInvalidCredentials_WhenEmailIsUnknown()
    {
        var handler = CreateHandler(
            new StubUserRepository(),
            out var accessTokens,
            out var refreshTokens,
            out var unitOfWork,
            out var passwordHasher
        );

        var result = await handler.Handle(new LoginCommand("ghost@example.com", "Password123!"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.InvalidCredentials, result.Error);
        Assert.Null(accessTokens.IssuedFor);
        Assert.Null(refreshTokens.IssuedFor);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        // The unknown-email path still pays for a hash + verify so failures are timing-indistinguishable.
        Assert.Equal(1, passwordHasher.VerifyCallCount);
    }

    [Fact]
    public async Task Handle_ReturnsInvalidCredentials_WhenPasswordIsWrong()
    {
        var users = new StubUserRepository { ExistingUser };
        var handler = CreateHandler(users, out _, out _, out var unitOfWork);

        var result = await handler.Handle(new LoginCommand("yao@example.com", "WrongPassword!"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.InvalidCredentials, result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static LoginCommandHandler CreateHandler(
        StubUserRepository users,
        out StubAccessTokenService accessTokens,
        out StubRefreshTokenService refreshTokens,
        out StubUnitOfWork unitOfWork
    ) => CreateHandler(users, out accessTokens, out refreshTokens, out unitOfWork, out _);

    private static LoginCommandHandler CreateHandler(
        StubUserRepository users,
        out StubAccessTokenService accessTokens,
        out StubRefreshTokenService refreshTokens,
        out StubUnitOfWork unitOfWork,
        out StubPasswordHasher passwordHasher
    )
    {
        accessTokens = new StubAccessTokenService();
        refreshTokens = new StubRefreshTokenService();
        unitOfWork = new StubUnitOfWork();
        passwordHasher = new StubPasswordHasher();
        return new LoginCommandHandler(users, unitOfWork, passwordHasher, accessTokens, refreshTokens);
    }

    private sealed class StubUserRepository : List<User>, IUserRepository
    {
        public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
            => Task.FromResult(this.FirstOrDefault(user => user.NormalizedEmail == normalizedEmail));

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(this.FirstOrDefault(user => user.Id == id));

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
    }

    private sealed class StubPasswordHasher : IPasswordHasher
    {
        public int VerifyCallCount { get; private set; }

        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string passwordHash)
        {
            VerifyCallCount++;
            return passwordHash == $"hashed:{password}";
        }
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
