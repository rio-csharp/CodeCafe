using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.GetCurrentUser;
using CodeCafe.Application.Common.Security;
using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Tests;

public sealed class GetCurrentUserQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCurrentUser()
    {
        var user = User.Create("Yao@Example.com", "yao@example.com", "Yao", "hash");
        var handler = new GetCurrentUserQueryHandler(
            new StubCurrentUserAccessor(new CurrentUser(user.Id)),
            new StubUserRepository { user }
        );

        var result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value!.Id);
        Assert.Equal(user.Email, result.Value.Email);
        Assert.Equal(user.DisplayName, result.Value.DisplayName);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenAccountWasDeleted()
    {
        var handler = new GetCurrentUserQueryHandler(
            new StubCurrentUserAccessor(new CurrentUser(Guid.CreateVersion7())),
            new StubUserRepository()
        );

        var result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenNoCurrentUser()
    {
        var handler = new GetCurrentUserQueryHandler(
            new StubCurrentUserAccessor(null),
            new StubUserRepository()
        );

        var result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.UserNotFound, result.Error);
    }

    private sealed class StubCurrentUserAccessor(CurrentUser? user) : ICurrentUserAccessor
    {
        public CurrentUser? User => user;
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
}
