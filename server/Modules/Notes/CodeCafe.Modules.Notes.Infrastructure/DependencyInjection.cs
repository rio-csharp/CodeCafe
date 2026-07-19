using CodeCafe.Modules.Identity.Application.Auth;
using CodeCafe.Shared.Application.Common.Interfaces;
using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Modules.Identity.Infrastructure.Identity;
using CodeCafe.Modules.Notes.Infrastructure.Notes;
using CodeCafe.Shared.Infrastructure.Persistence;
using CodeCafe.Modules.Notes.Infrastructure.Services;
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
            options.UseNpgsql(connectionString);
            options.UseOpenIddict<Guid>();
        });
        services.AddScoped<IAuthUserGateway, IdentityAuthUserGateway>();
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
