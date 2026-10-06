using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.PersonalAccessTokens.ListPersonalAccessTokens;
using CodeCafe.Application.Auth.PersonalAccessTokens.Shared;
using CodeCafe.Application.Common.Security;
using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Tests.Auth;

public sealed class ListPersonalAccessTokensQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyOwnTokens_IncludingRevokedOnes()
    {
        var userId = Guid.CreateVersion7();
        var active = PersonalAccessToken.Create(userId, "mcp", "hash-a", DateTimeOffset.UtcNow.AddDays(90));
        var revoked = PersonalAccessToken.Create(userId, "old-ci", "hash-b", DateTimeOffset.UtcNow.AddDays(90));
        revoked.Revoke(DateTimeOffset.UtcNow.AddDays(-1));
        var foreign = PersonalAccessToken.Create(Guid.CreateVersion7(), "not-mine", "hash-c", DateTimeOffset.UtcNow.AddDays(90));
        var tokens = new StubPersonalAccessTokenRepository { active, revoked, foreign };
        var handler = CreateHandler(userId, tokens);

        var result = await handler.Handle(new ListPersonalAccessTokensQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        var revokedDto = Assert.Single(result.Value.Where(dto => dto.Id == revoked.Id));
        Assert.NotNull(revokedDto.RevokedAtUtc);
        Assert.Equal(revoked.RevokedAtUtc, revokedDto.RevokedAtUtc);
        Assert.Single(result.Value.Where(dto => dto.Id == active.Id));
    }

    [Fact]
    public async Task Handle_MapsMetadata_ButNeverTheHash()
    {
        var userId = Guid.CreateVersion7();
        var token = PersonalAccessToken.Create(userId, "mcp", "hash-a", DateTimeOffset.UtcNow.AddDays(90));
        var handler = CreateHandler(userId, new StubPersonalAccessTokenRepository { token });

        var result = await handler.Handle(new ListPersonalAccessTokensQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value!);
        Assert.Equal(token.Id, dto.Id);
        Assert.Equal(token.Name, dto.Name);
        Assert.Equal(token.CreatedAtUtc, dto.CreatedAtUtc);
        Assert.Equal(token.ExpiresAtUtc, dto.ExpiresAtUtc);
        Assert.Null(dto.RevokedAtUtc);
        // Compile-time shape check made explicit: the list DTO has no hash or raw-token member.
        Assert.Null(typeof(PersonalAccessTokenDto).GetProperty("TokenHash"));
        Assert.Null(typeof(PersonalAccessTokenDto).GetProperty("Token"));
    }

    [Fact]
    public async Task Handle_ReturnsUserNotFound_WhenNoCurrentUser()
    {
        var handler = new ListPersonalAccessTokensQueryHandler(
            new StubCurrentUserAccessor(null),
            new StubPersonalAccessTokenRepository()
        );

        var result = await handler.Handle(new ListPersonalAccessTokensQuery(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.UserNotFound, result.Error);
    }

    private static ListPersonalAccessTokensQueryHandler CreateHandler(Guid userId, StubPersonalAccessTokenRepository tokens)
        => new(new StubCurrentUserAccessor(new CurrentUser(userId)), tokens);

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
}
