using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.PersonalAccessTokens.CreatePersonalAccessToken;
using CodeCafe.Application.Auth.PersonalAccessTokens.Shared;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Security;
using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Tests.Auth;

public sealed class CreatePersonalAccessTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_StoresHash_NotRawToken()
    {
        var tokens = new StubPersonalAccessTokenRepository();
        var handler = CreateHandler(tokens);

        var result = await handler.Handle(new CreatePersonalAccessTokenCommand("mcp", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var rawToken = result.Value!.Token;
        var stored = Assert.Single(tokens);
        Assert.StartsWith(PersonalAccessTokenHash.Prefix, rawToken);
        Assert.NotEqual(rawToken, stored.TokenHash);
        Assert.Equal(PersonalAccessTokenHash.Compute(rawToken), stored.TokenHash);
    }

    [Fact]
    public async Task Handle_ReturnsRawToken_InResponseOnly()
    {
        var tokens = new StubPersonalAccessTokenRepository();
        var handler = CreateHandler(tokens);

        var result = await handler.Handle(new CreatePersonalAccessTokenCommand("mcp", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // The raw token appears exactly once: in the creation response, nowhere else.
        Assert.DoesNotContain(result.Value!.Token, result.Value.ToString());
        Assert.Null(typeof(PersonalAccessTokenDto).GetProperty("Token"));
    }

    [Fact]
    public async Task Handle_DefaultsTo90Days()
    {
        var tokens = new StubPersonalAccessTokenRepository();
        var handler = CreateHandler(tokens);

        var result = await handler.Handle(new CreatePersonalAccessTokenCommand("mcp", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.InRange((result.Value!.ExpiresAtUtc - DateTimeOffset.UtcNow).TotalDays, 89.9, 90.1);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1000, 365)]
    [InlineData(30, 30)]
    public async Task Handle_ClampsExpiry_ToOneTo365Days(int expiresInDays, int expectedDays)
    {
        var tokens = new StubPersonalAccessTokenRepository();
        var handler = CreateHandler(tokens);

        var result = await handler.Handle(new CreatePersonalAccessTokenCommand("mcp", expiresInDays), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.InRange(
            (result.Value!.ExpiresAtUtc - DateTimeOffset.UtcNow).TotalDays,
            expectedDays - 0.1,
            expectedDays + 0.1
        );
    }

    [Fact]
    public async Task Handle_ReturnsUserNotFound_WhenNoCurrentUser()
    {
        var handler = new CreatePersonalAccessTokenCommandHandler(
            new StubCurrentUserAccessor(null),
            new StubPersonalAccessTokenRepository(),
            new StubUnitOfWork()
        );

        var result = await handler.Handle(new CreatePersonalAccessTokenCommand("mcp", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.UserNotFound, result.Error);
    }

    private static CreatePersonalAccessTokenCommandHandler CreateHandler(StubPersonalAccessTokenRepository tokens)
        => new(
            new StubCurrentUserAccessor(new CurrentUser(Guid.CreateVersion7())),
            tokens,
            new StubUnitOfWork()
        );

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
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken)
            => Task.FromResult<ITransaction>(StubTransaction.Instance);
    }
}
