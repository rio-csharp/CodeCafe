using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Host.Hosting;

internal static class DatabaseMigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        // Single-instance deployment: applying pending migrations at startup keeps deploys
        // zero-touch. If the app ever scales out, move this to a deploy step instead.
        // Tests set Database:MigrateOnStartup = false so they boot without a database.
        if (!app.Configuration.GetValue("Database:MigrateOnStartup", defaultValue: true))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
