using CodeCafe.Shared.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CodeCafe.Shared.Infrastructure.Persistence;

public sealed class ApplicationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Never fall back to guessed local credentials: a machine with a
            // default postgres install would be silently targeted instead.
            throw new InvalidOperationException(
                "No connection string available for design-time DbContext creation. " +
                "Set the ConnectionStrings__DefaultConnection environment variable, e.g. " +
                "\"Host=localhost;Port=5432;Database=codecafe;Username=<user>;Password=<password>\".");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        optionsBuilder.UseOpenIddict<Guid>();

        return new ApplicationDbContext(optionsBuilder.Options, new DesignTimeDateTimeProvider());
    }

    private sealed class DesignTimeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
