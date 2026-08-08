using CodeCafe.Application.Notes;
using CodeCafe.Modules.Notes.Infrastructure.Notes;
using CodeCafe.Modules.Notes.Infrastructure.Services;
using CodeCafe.Application.Common;
using CodeCafe.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CodeCafe.Modules.Notes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString) && !environment.IsEnvironment("Testing"))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");
        }

        connectionString ??= "Host=localhost;Database=codecafe_testing_placeholder";

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            // Pin the migrations assembly explicitly. Without this, EF resolves migrations from the
            // assembly holding the DbContext, so moving ApplicationDbContext to another project would
            // silently orphan the applied migrations: EF would find none and try to run
            // InitialIdentity against a populated database. Pinning keeps the existing migrations,
            // their namespace and their MigrationIds untouched no matter where the context lives.
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(ApplicationDbContextAssembly.Name));
            options.UseOpenIddict<Guid>();
        });
        services.AddScoped<INotebookReadService, NotebookReadService>();
        services.AddScoped<INotebookMutationStore, NotebookMutationStore>();
        services.AddScoped<INotebookItemMutationService, NotebookItemMutationService>();
        services.AddSingleton<IMcpIndependentAuditQueue, McpIndependentAuditQueue>();
        services.AddHostedService(serviceProvider => (McpIndependentAuditQueue)serviceProvider.GetRequiredService<IMcpIndependentAuditQueue>());
        services.AddScoped<IMcpAuditService, McpAuditService>();
        services.AddSingleton<ITipTapPlainTextExtractor, TipTapPlainTextExtractor>();
        services.AddSingleton<ITipTapContentService, TipTapContentService>();

        return services;
    }
}
