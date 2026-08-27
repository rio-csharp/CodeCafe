using CodeCafe.Application.Common;
using CodeCafe.Infrastructure.Notes;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Tests;

/// <summary>
/// Spins up an isolated SQLite in-memory <see cref="ApplicationDbContext"/> so the real
/// EF-backed Notes services can be exercised. The connection is kept open for the lifetime
/// of the harness because an in-memory SQLite database only exists while a connection to it
/// is open.
/// </summary>
internal sealed class NotesDbHarness : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public NotesDbHarness(MutableDateTimeProvider? dateTimeProvider = null)
    {
        DateTimeProvider = dateTimeProvider ?? new MutableDateTimeProvider();
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public MutableDateTimeProvider DateTimeProvider { get; }

    public ApplicationDbContext CreateContext() => new(_options, DateTimeProvider);

    public NotebookReadService CreateReadService(ApplicationDbContext context) => new(context);

    public NotebookItemMutationService CreateMutationService(ApplicationDbContext context) =>
        new(context, DateTimeProvider, new TipTapContentService());

    public void Dispose()
    {
        _connection.Dispose();
    }
}

internal sealed class MutableDateTimeProvider(DateTimeOffset? initial = null) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } =
        initial ?? DateTimeOffset.Parse("2026-06-01T00:00:00+00:00");
}
