using System.Text.RegularExpressions;

namespace CodeCafe.Infrastructure.Tests;

/// <summary>
/// A plain CREATE INDEX holds an ACCESS EXCLUSIVE lock and blocks writes for the whole build, which is
/// a write outage when the table is large. Index builds on growable tables must therefore use
/// CREATE INDEX CONCURRENTLY, which needs suppressTransaction because it cannot run inside one.
/// </summary>
public sealed class MigrationIndexConcurrencyTests
{
    /// <summary>
    /// Already applied to test and production before this rule existed. Editing it cannot undo the lock
    /// that already happened, and only fresh (empty) databases would see the change, so it stays.
    /// </summary>
    private static readonly HashSet<string> GrandfatheredMigrations = new(StringComparer.Ordinal)
    {
        "20260718224927_AddNotebookTrigramIndexes.cs"
    };

    [Fact]
    public void Migrations_DoNotBuildGinIndexes_WithoutConcurrently()
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateMigrationFiles())
        {
            var name = Path.GetFileName(file);
            if (GrandfatheredMigrations.Contains(name))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            var declaresGinIndex = source.Contains("Npgsql:IndexMethod", StringComparison.Ordinal)
                || Regex.IsMatch(source, @"USING\s+gin", RegexOptions.IgnoreCase);
            if (!declaresGinIndex)
            {
                continue;
            }

            if (!source.Contains("CONCURRENTLY", StringComparison.OrdinalIgnoreCase))
            {
                offenders.Add(name);
            }
        }

        Assert.True(
            offenders.Count == 0,
            "These migrations build a GIN index without CONCURRENTLY, which blocks writes for the "
            + "duration of the build. Use migrationBuilder.Sql(\"CREATE INDEX CONCURRENTLY IF NOT "
            + $"EXISTS ...\", suppressTransaction: true) instead: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Migrations_UsingConcurrently_SuppressTheTransaction()
    {
        // CREATE INDEX CONCURRENTLY throws if it runs inside a transaction, and EF wraps each migration
        // in one by default, so the two must always appear together.
        var offenders = EnumerateMigrationFiles()
            .Where(file =>
            {
                var source = File.ReadAllText(file);
                return source.Contains("CONCURRENTLY", StringComparison.OrdinalIgnoreCase)
                    && !source.Contains("suppressTransaction", StringComparison.Ordinal);
            })
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "CREATE INDEX CONCURRENTLY cannot run inside a transaction; pass suppressTransaction: true "
            + $"to migrationBuilder.Sql in: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void GrandfatheredMigrations_StillExist()
    {
        // Keeps the allowlist honest: a renamed or deleted migration must not silently keep exempting
        // a file that no longer exists.
        var names = EnumerateMigrationFiles().Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal);

        foreach (var grandfathered in GrandfatheredMigrations)
        {
            Assert.Contains(grandfathered, names);
        }
    }

    private static IEnumerable<string> EnumerateMigrationFiles()
    {
        var directory = FindMigrationsDirectory();
        return Directory.EnumerateFiles(directory, "*.cs")
            .Where(file => !file.EndsWith(".Designer.cs", StringComparison.Ordinal)
                && !Path.GetFileName(file).Equals("ApplicationDbContextModelSnapshot.cs", StringComparison.Ordinal));
    }

    private static string FindMigrationsDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "server",
                "src",
                "CodeCafe.Infrastructure",
                "Persistence",
                "Migrations");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the EF migrations directory from the test output path.");
    }
}
