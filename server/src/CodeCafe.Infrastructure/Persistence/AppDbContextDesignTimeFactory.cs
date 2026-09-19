using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CodeCafe.Infrastructure.Persistence;

// Used only by `dotnet ef`, which runs without a host. Configuration mirrors the host's
// sources; a missing connection string fails fast instead of silently using a default.
public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // dotnet ef is a developer activity; default to Development (environment variables still win below).
        var environment =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // CWD is the startup project, so appsettings may live here or in the host project.
        var basePaths = new[]
        {
            Directory.GetCurrentDirectory(),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "CodeCafe.Host")),
        };

        var builder = new ConfigurationBuilder();
        foreach (var basePath in basePaths.Distinct())
        {
            builder
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true);
        }
        builder.AddEnvironmentVariables();
        var configuration = builder.Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. "
                + "Add it to appsettings.json or set the ConnectionStrings__DefaultConnection "
                + "environment variable before running dotnet ef commands that need a database.");

        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options;
        return new AppDbContext(options);
    }
}
