using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Server.Infrastructure;

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
                await dbContext.Database.ExecuteSqlRawAsync(
                    $"select pg_advisory_unlock({MigrationLockId})",
                    cancellationToken);
            }
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }

        logger.LogInformation("Database migrations applied.");
    }
}
