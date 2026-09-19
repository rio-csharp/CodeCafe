using CodeCafe.Infrastructure.Persistence;
using DotNet.Testcontainers.Configurations;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CodeCafe.Infrastructure.Tests;

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}

// One container per test run shared by all tests in the collection; the schema is empty, so
// per-test EnsureDeleted/EnsureCreated is cheap and gives each test a clean database.
public sealed class PostgresFixture : IAsyncLifetime
{
    static PostgresFixture()
    {
        // Ryuk (the Testcontainers cleanup sidecar) cannot be pulled from Docker Hub on this
        // network; the fixture disposes its own container, so the reaper is redundant here.
        TestcontainersSettings.ResourceReaperEnabled = false;
    }

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("codecafe_tests")
        .WithUsername("codecafe")
        .WithPassword("codecafe")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    public async Task<AppDbContext> CreateCleanContextAsync()
    {
        var dbContext = CreateContext();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    // Same context shape without wiping the schema, for tests that re-open a second context
    // to verify what a previous one persisted.
    public async Task<AppDbContext> CreateContextAsync()
    {
        var dbContext = CreateContext();
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }
}
