using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using CodeCafe.Domain.Notes.Enums;
using CodeCafe.Domain.Notes.ValueObjects;
using CodeCafe.Infrastructure.Identity;
using CodeCafe.Infrastructure.Notes.Read;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CodeCafe.Infrastructure.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    static PostgresFixture()
    {
        // Ryuk (Testcontainers' cleanup sidecar) requires pulling an extra image, which is
        // blocked in some network environments; the fixture disposes the container itself.
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
    }

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;

internal sealed class NotesDbHarness : IDisposable
{
    // Guid-derived name, no user input reaches this DDL.
    private readonly string _databaseName = "codecafe_test_" + Guid.NewGuid().ToString("N");
    private readonly string _serverConnectionString;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public NotesDbHarness(PostgresFixture fixture)
    {
        TimeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        _serverConnectionString = fixture.ConnectionString;

        using (var master = new NpgsqlConnection(_serverConnectionString))
        {
            master.Open();
            using var create = master.CreateCommand();
            create.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
            create.ExecuteNonQuery();
        }

        var builder = new NpgsqlConnectionStringBuilder(_serverConnectionString)
        {
            Database = _databaseName,
        };
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(builder.ConnectionString)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public FakeTimeProvider TimeProvider { get; }

    public ApplicationDbContext CreateContext() => new(_options, TimeProvider);

    public NotebookReadService CreateReadService(ApplicationDbContext context) =>
        new(context, new FakeAccessCodeHasher());

    public void SeedAccessCode(ApplicationDbContext context, Notebook notebook, string accessCode)
    {
        var violation = notebook.SetAccessCode(FakeAccessCodeHasher.HashCode(accessCode));
        Assert.Null(violation);
        context.SaveChanges();
    }

    public void SeedUser(ApplicationDbContext context, Guid userId, string displayName)
    {
        context.Users.Add(
            new ApplicationUser
            {
                Id = userId,
                DisplayName = displayName,
                UserName = displayName,
                NormalizedUserName = displayName.ToUpperInvariant(),
            }
        );
        context.SaveChanges();
    }

    public Notebook SeedNotebook(
        ApplicationDbContext context,
        Guid ownerId,
        string title,
        string slug,
        NotebookVisibility visibility = NotebookVisibility.Private
    )
    {
        var notebook = Notebook.Create(
            Guid.NewGuid(),
            ownerId,
            title,
            NotebookSlug.Create(slug),
            null,
            visibility,
            TimeProvider.GetUtcNow()
        );
        if (visibility == NotebookVisibility.Public)
        {
            notebook.ApplyVisibility(NotebookVisibility.Public, TimeProvider.GetUtcNow());
        }

        context.Notebooks.Add(notebook);
        context.SaveChanges();
        return notebook;
    }

    public Guid SeedPage(
        ApplicationDbContext context,
        Notebook notebook,
        string title,
        string? slug = null,
        Guid? parentId = null
    )
    {
        var pageId = Guid.NewGuid();
        var violation = notebook.AddItem(
            pageId,
            NotebookItemType.Page,
            title,
            slug is null ? null : NotebookSlug.Create(slug),
            parentId,
            1,
            TimeProvider.GetUtcNow()
        );
        Assert.Null(violation);
        context.SaveChanges();
        return pageId;
    }

    public void SeedFavorite(ApplicationDbContext context, Guid notebookId, Guid userId)
    {
        context.NotebookFavorites.Add(
            NotebookFavorite.Create(Guid.NewGuid(), notebookId, userId, TimeProvider.GetUtcNow())
        );
        context.SaveChanges();
    }

    public void SeedPageContent(ApplicationDbContext context, Guid pageId, string text)
    {
        var item = context.NotebookItems.Find(pageId)!;
        // Test texts are JSON-safe (plain ASCII), so inline interpolation is fine here.
        item.SetPageContent(
            $$"""{"type":"doc","content":[{"type":"paragraph","content":[{"type":"text","text":"{{text}}"}]}]}"""
        );
        context.SaveChanges();
    }

    public void SeedShare(ApplicationDbContext context, Guid notebookId, Guid userId, Guid grantedBy)
    {
        context.NotebookShares.Add(
            NotebookShare.Create(Guid.NewGuid(), notebookId, userId, grantedBy, TimeProvider.GetUtcNow())
        );
        context.SaveChanges();
    }

    public void Dispose()
    {
        using var master = new NpgsqlConnection(_serverConnectionString);
        master.Open();
        using var drop = master.CreateCommand();
        drop.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)";
        drop.ExecuteNonQuery();
    }
}

internal sealed class FakeAccessCodeHasher : INotebookAccessCodeHasher
{
    public static string HashCode(string accessCode) => "hashed:" + accessCode;

    public string Hash(string accessCode) => HashCode(accessCode);

    public bool Verify(string accessCodeHash, string providedCode) =>
        accessCodeHash == HashCode(providedCode);
}
