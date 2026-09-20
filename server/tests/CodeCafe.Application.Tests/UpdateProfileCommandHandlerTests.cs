using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.UpdateProfile;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Security;
using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Tests;

public sealed class UpdateProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_RenamesUserAndReturnsUpdatedDto()
    {
        var user = User.Create("yao@example.com", "yao@example.com", "Yao", "hash");
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(user, unitOfWork);

        var result = await handler.Handle(new UpdateProfileCommand("  New Name  "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", user.DisplayName);
        Assert.Equal(user.Id, result.Value!.Id);
        Assert.Equal(user.Email, result.Value.Email);
        Assert.Equal("New Name", result.Value.DisplayName);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenAccountWasDeleted()
    {
        var unitOfWork = new StubUnitOfWork();
        var handler = new UpdateProfileCommandHandler(
            new StubCurrentUserAccessor(new CurrentUser(Guid.CreateVersion7())),
            new StubUserRepository(),
            unitOfWork
        );

        var result = await handler.Handle(new UpdateProfileCommand("New Name"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.UserNotFound, result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static UpdateProfileCommandHandler CreateHandler(User user, StubUnitOfWork unitOfWork)
        => new(
            new StubCurrentUserAccessor(new CurrentUser(user.Id)),
            new StubUserRepository { user },
            unitOfWork
        );

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

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
