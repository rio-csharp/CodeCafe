using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Host.Hosting;

internal static class DatabaseMigrationExtensions
{
    /// <param name="ignoreStartupSetting">
    /// Set by the `migrate` verb. Kubernetes keeps Database:MigrateOnStartup=false on the
    /// replicas and migrates through a pre-upgrade Job instead, so the Job itself must not be
    /// gated by the setting that exists to stop the replicas from migrating.
    /// </param>
    public static async Task ApplyMigrationsAsync(
        this WebApplication app,
        bool ignoreStartupSetting = false
    )
    {
        // Local development and single-replica hosts migrate on start-up, which keeps deploys
        // zero-touch. Kubernetes turns this off and migrates through the `migrate` Job instead,
        // because its replicas scale out. Tests set Database:MigrateOnStartup = false so they
        // boot without a database.
        if (
            !ignoreStartupSetting
            && !app.Configuration.GetValue("Database:MigrateOnStartup", defaultValue: true)
        )
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
