using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Host.Common;

public sealed class DatabaseMigrationRunner(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<DatabaseMigrationRunner> logger)
{
    private const long MigrationLockId = 0x434F444543414645;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Applying database migrations.");

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (dbContext.Database.ProviderName != DatabaseProviderNames.Npgsql)
        {
            // pg_advisory_lock is PostgreSQL-only; other providers migrate directly.
            await dbContext.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations applied.");
            return;
        }

        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                $"select pg_advisory_lock({MigrationLockId})",
                cancellationToken);

            try
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
            }
            finally
            {
                // The lock must be released even when the caller cancelled
                // mid-migration, so the unlock runs without cancellation.
                await dbContext.Database.ExecuteSqlRawAsync(
                    $"select pg_advisory_unlock({MigrationLockId})",
                    CancellationToken.None);
            }
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }

        logger.LogInformation("Database migrations applied.");
    }
}
