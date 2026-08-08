using CodeCafe.Modules.Identity.Presentation.Auth;
using CodeCafe.Server.Infrastructure;
using CodeCafe.Application.Common;
using CodeCafe.Shared.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using Xunit;

namespace CodeCafe.Server.Tests;

/// <summary>
/// Exercises <see cref="DynamicClientCleanupHostedService"/> against a real SQLite database with
/// foreign key enforcement on, mirroring how the production Npgsql schema links tokens and
/// authorizations to applications without cascade deletes.
/// </summary>
public sealed class DynamicClientCleanupHostedServiceTests
{
    [Fact]
    public async Task CleanupAsync_RemovesStaleDynamicClient_WithItsTokens()
    {
        await using var fixture = await CleanupFixture.CreateAsync();
        var applicationId = fixture.AddApplication(OpenIddictClientRegistration.CreateDynamicClientId());
        fixture.AddToken(applicationId, DateTimeOffset.UtcNow.AddDays(-31));
        fixture.AddToken(applicationId, DateTimeOffset.UtcNow.AddDays(-40));
        await fixture.SaveSeedAsync();

        await fixture.RunCleanupAsync();

        Assert.False(await fixture.ApplicationExistsAsync(applicationId));
        Assert.Equal(0, await fixture.TokenCountAsync(applicationId));
    }

    [Fact]
    public async Task CleanupAsync_KeepsDynamicClient_WithAStillValidToken()
    {
        await using var fixture = await CleanupFixture.CreateAsync();
        var applicationId = fixture.AddApplication(OpenIddictClientRegistration.CreateDynamicClientId());
        fixture.AddToken(applicationId, DateTimeOffset.UtcNow.AddDays(-31));
        fixture.AddToken(applicationId, DateTimeOffset.UtcNow.AddDays(1));
        await fixture.SaveSeedAsync();

        await fixture.RunCleanupAsync();

        Assert.True(await fixture.ApplicationExistsAsync(applicationId));
        Assert.Equal(2, await fixture.TokenCountAsync(applicationId));
    }

    [Fact]
    public async Task CleanupAsync_KeepsDynamicClient_WithoutTokens()
    {
        await using var fixture = await CleanupFixture.CreateAsync();
        var applicationId = fixture.AddApplication(OpenIddictClientRegistration.CreateDynamicClientId());
        await fixture.SaveSeedAsync();

        await fixture.RunCleanupAsync();

        Assert.True(await fixture.ApplicationExistsAsync(applicationId));
    }

    [Fact]
    public async Task CleanupAsync_KeepsStaticClient_EvenWithOnlyExpiredTokens()
    {
        await using var fixture = await CleanupFixture.CreateAsync();
        var applicationId = fixture.AddApplication("codecafe-prod");
        fixture.AddToken(applicationId, DateTimeOffset.UtcNow.AddDays(-31));
        await fixture.SaveSeedAsync();

        await fixture.RunCleanupAsync();

        Assert.True(await fixture.ApplicationExistsAsync(applicationId));
        Assert.Equal(1, await fixture.TokenCountAsync(applicationId));
    }

    [Fact]
    public async Task CleanupAsync_ContinuesWithRemainingClients_WhenOneDeleteFails()
    {
        await using var fixture = await CleanupFixture.CreateAsync();
        var failingApplicationId = fixture.AddApplication(OpenIddictClientRegistration.CreateDynamicClientId());
        fixture.AddToken(failingApplicationId, DateTimeOffset.UtcNow.AddDays(-31));
        var deletableApplicationId = fixture.AddApplication(OpenIddictClientRegistration.CreateDynamicClientId());
        fixture.AddToken(deletableApplicationId, DateTimeOffset.UtcNow.AddDays(-31));
        await fixture.SaveSeedAsync();

        await fixture.RunCleanupAsync(failDeleteForApplicationId: failingApplicationId);

        Assert.True(await fixture.ApplicationExistsAsync(failingApplicationId));
        Assert.False(await fixture.ApplicationExistsAsync(deletableApplicationId));
        Assert.Equal(0, await fixture.TokenCountAsync(deletableApplicationId));
    }

    private sealed class CleanupFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _serviceProvider;
        private readonly ApplicationDbContext _seedContext;
        private readonly DeleteFailureGate _deleteFailureGate;

        private CleanupFixture(
            SqliteConnection connection,
            ServiceProvider serviceProvider,
            ApplicationDbContext seedContext,
            DeleteFailureGate deleteFailureGate)
        {
            _connection = connection;
            _serviceProvider = serviceProvider;
            _seedContext = seedContext;
            _deleteFailureGate = deleteFailureGate;
        }

        public static async Task<CleanupFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IDateTimeProvider, TestDateTimeProvider>();
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(connection);
                options.UseOpenIddict<Guid>();
            });
            services.AddOpenIddict()
                .AddCore(options =>
                {
                    options.UseEntityFrameworkCore()
                        .UseDbContext<ApplicationDbContext>()
                        .ReplaceDefaultEntities<Guid>();
                });

            // Registered after AddCore so it wins resolution: delegates everything to the
            // real manager except deletes for the gated application id, which throw.
            services.AddSingleton(new DeleteFailureGate());
            services.AddScoped<IOpenIddictApplicationManager>(serviceProvider => new ThrowingDeleteApplicationManager(
                serviceProvider.GetRequiredService<IOpenIddictApplicationCache<OpenIddictEntityFrameworkCoreApplication<Guid>>>(),
                serviceProvider.GetRequiredService<ILogger<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication<Guid>>>>(),
                serviceProvider.GetRequiredService<IOptionsMonitor<OpenIddictCoreOptions>>(),
                serviceProvider.GetRequiredService<IOpenIddictApplicationStore<OpenIddictEntityFrameworkCoreApplication<Guid>>>(),
                serviceProvider.GetRequiredService<DeleteFailureGate>()));

            var serviceProvider = services.BuildServiceProvider();
            var seedContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            await seedContext.Database.EnsureCreatedAsync();

            return new CleanupFixture(connection, serviceProvider, seedContext, serviceProvider.GetRequiredService<DeleteFailureGate>());
        }

        public Guid AddApplication(string clientId)
        {
            var application = new OpenIddictEntityFrameworkCoreApplication<Guid>
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                ClientType = "public",
                ApplicationType = "native",
                ConsentType = "implicit",
                DisplayName = clientId,
                ConcurrencyToken = Guid.NewGuid().ToString("N")
            };
            _seedContext.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>().Add(application);
            return application.Id;
        }

        public void AddToken(Guid applicationId, DateTimeOffset expiresAtUtc)
        {
            _seedContext.Set<OpenIddictEntityFrameworkCoreToken<Guid>>().Add(
                new OpenIddictEntityFrameworkCoreToken<Guid>
                {
                    Id = Guid.NewGuid(),
                    Application = _seedContext.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>().Local.Single(application => application.Id == applicationId),
                    ConcurrencyToken = Guid.NewGuid().ToString("N"),
                    Type = "refresh_token",
                    Status = "valid",
                    Subject = "test-user",
                    CreationDate = DateTimeOffset.UtcNow.AddDays(-60).UtcDateTime,
                    ExpirationDate = expiresAtUtc.UtcDateTime
                });
        }

        public Task SaveSeedAsync() => _seedContext.SaveChangesAsync();

        public async Task RunCleanupAsync(Guid? failDeleteForApplicationId = null)
        {
            _deleteFailureGate.ApplicationId = failDeleteForApplicationId;
            var service = new DynamicClientCleanupHostedService(
                _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<DynamicClientCleanupHostedService>.Instance);
            await service.CleanupAsync(CancellationToken.None);
        }

        public async Task<bool> ApplicationExistsAsync(Guid applicationId)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>()
                .AnyAsync(application => application.Id == applicationId);
        }

        public async Task<int> TokenCountAsync(Guid applicationId)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await dbContext.Set<OpenIddictEntityFrameworkCoreToken<Guid>>()
                .CountAsync(token => token.Application != null && token.Application.Id == applicationId);
        }

        public async ValueTask DisposeAsync()
        {
            await _seedContext.DisposeAsync();
            await _serviceProvider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class DeleteFailureGate
    {
        public Guid? ApplicationId { get; set; }
    }

    private sealed class ThrowingDeleteApplicationManager(
        IOpenIddictApplicationCache<OpenIddictEntityFrameworkCoreApplication<Guid>> cache,
        ILogger<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication<Guid>>> logger,
        IOptionsMonitor<OpenIddictCoreOptions> options,
        IOpenIddictApplicationStore<OpenIddictEntityFrameworkCoreApplication<Guid>> store,
        DeleteFailureGate deleteFailureGate)
        : OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication<Guid>>(cache, logger, options, store)
    {
        public override ValueTask DeleteAsync(
            OpenIddictEntityFrameworkCoreApplication<Guid> application,
            CancellationToken cancellationToken = default)
        {
            if (application.Id == deleteFailureGate.ApplicationId)
            {
                throw new InvalidOperationException("Simulated delete failure for tests.");
            }

            return base.DeleteAsync(application, cancellationToken);
        }
    }
}
