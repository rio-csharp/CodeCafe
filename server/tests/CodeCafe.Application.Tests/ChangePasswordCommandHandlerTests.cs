using CodeCafe.Application.Auth;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.ChangePassword;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Security;
using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Tests;

public sealed class ChangePasswordCommandHandlerTests
{
    private static readonly ChangePasswordCommand Command = new("OldPassword1!", "NewPassword1!");

    [Fact]
    public async Task Handle_ChangesPasswordHash()
    {
        var user = User.Create("yao@example.com", "yao@example.com", "Yao", "hashed:OldPassword1!");
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(user, unitOfWork);

        var result = await handler.Handle(Command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("hashed:NewPassword1!", user.PasswordHash);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);

        var domainEvent = Assert.IsType<PasswordChangedEvent>(Assert.Single(user.DomainEvents));
        Assert.Equal(user.Id, domainEvent.UserId);
    }

    [Fact]
    public async Task Handle_ReturnsUnauthorized_WhenCurrentPasswordIsWrong()
    {
        var user = User.Create("yao@example.com", "yao@example.com", "Yao", "hashed:OldPassword1!");
        var unitOfWork = new StubUnitOfWork();
        var handler = CreateHandler(user, unitOfWork);

        var result = await handler.Handle(Command with { CurrentPassword = "WrongPassword1!" }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.IncorrectCurrentPassword, result.Error);
        Assert.Equal("hashed:OldPassword1!", user.PasswordHash);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenAccountWasDeleted()
    {
        var unitOfWork = new StubUnitOfWork();
        var handler = new ChangePasswordCommandHandler(
            new StubCurrentUserAccessor(new CurrentUser(Guid.CreateVersion7())),
            new StubUserRepository(),
            unitOfWork,
            new StubPasswordHasher()
        );

        var result = await handler.Handle(Command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrors.UserNotFound, result.Error);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static ChangePasswordCommandHandler CreateHandler(User user, StubUnitOfWork unitOfWork)
        => new(
            new StubCurrentUserAccessor(new CurrentUser(user.Id)),
            new StubUserRepository { user },
            unitOfWork,
            new StubPasswordHasher()
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
    }

    private sealed class StubPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string passwordHash) => passwordHash == $"hashed:{password}";
    }
}
