using CodeCafe.Application.Auth.ChangePassword;
using CodeCafe.Domain.Identity;
using CodeCafe.Infrastructure.Auth;
using CodeCafe.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CodeCafe.Infrastructure.Tests.Persistence;

[Collection(nameof(PostgresCollection))]
public sealed class DomainEventDispatchTests(PostgresFixture fixture)
{
    [Fact]
    public async Task SaveChanges_PublishesRaisedEvents_AndDrainsTheBuffer()
    {
        var publisher = new RecordingPublisher();
        await using var dbContext = await fixture.CreateCleanContextAsync(publisher);
        var user = User.Create("dispatch@example.com", "dispatch@example.com", "Dispatch", "hash");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Empty(publisher.Published);

        user.ChangePasswordHash("new-hash");
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var published = Assert.IsType<PasswordChangedEvent>(Assert.Single(publisher.Published));
        Assert.Equal(user.Id, published.UserId);
        Assert.Empty(user.DomainEvents);
    }

    [Fact]
    public async Task SaveChanges_CommitsEventReactionsInTheSameTransaction()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        AppDbContext? dbContext = null;
        var publisher = new DelegatePublisher(async (passwordChanged, cancellationToken) =>
        {
            // MediatR resolves the subscriber in production; here it is wired by hand.
            var refreshTokens = new RefreshTokenService(dbContext!, Options.Create(new AuthOptions()));
            await new RevokeSessionsOnPasswordChanged(refreshTokens).Handle(passwordChanged, cancellationToken);
        });

        dbContext = new AppDbContext(options, publisher);
        await using (dbContext)
        {
            await dbContext.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
            await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

            var user = User.Create("reaction@example.com", "reaction@example.com", "Reaction", "hash");
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var refreshTokens = new RefreshTokenService(dbContext, Options.Create(new AuthOptions()));
            await refreshTokens.IssueAsync(user.Id, CancellationToken.None);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            user.ChangePasswordHash("new-hash");
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            await using var verification = new AppDbContext(options, NullPublisher.Instance);
            var storedUser = await verification.Users.SingleAsync(
                candidate => candidate.Id == user.Id,
                TestContext.Current.CancellationToken
            );
            Assert.Equal("new-hash", storedUser.PasswordHash);

            var storedToken = await verification.RefreshTokens.SingleAsync(
                candidate => candidate.UserId == user.Id,
                TestContext.Current.CancellationToken
            );
            Assert.NotNull(storedToken.RevokedAtUtc);
        }
    }

    private sealed class RecordingPublisher : IPublisher
    {
        public List<object> Published { get; } = [];

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Published.Add(notification);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Publish((object)notification!, cancellationToken);
    }

    private sealed class DelegatePublisher(
        Func<PasswordChangedEvent, CancellationToken, Task> onPasswordChanged
    ) : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => notification is PasswordChangedEvent passwordChanged
                ? onPasswordChanged(passwordChanged, cancellationToken)
                : Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Publish((object)notification!, cancellationToken);
    }
}
