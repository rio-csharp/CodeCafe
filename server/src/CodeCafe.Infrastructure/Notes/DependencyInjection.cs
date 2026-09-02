using CodeCafe.Application.Notes;
using CodeCafe.Infrastructure.Notes.Read;
using CodeCafe.Infrastructure.Notes.Write;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Infrastructure.Notes;

public static class DependencyInjection
{
    public static IServiceCollection AddNotesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is required."
            );
        }

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<DispatchDomainEventsInterceptor>();
        services.AddDbContext<ApplicationDbContext>((provider, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(provider.GetRequiredService<DispatchDomainEventsInterceptor>());
        });
        services.AddSingleton<INotebookAccessCodeHasher, NotebookAccessCodeHasher>();
        services.AddScoped<INotebookReadService, NotebookReadService>();
        services.AddScoped<INotebookRepository, NotebookRepository>();
        services.AddScoped<INotebookSlugGenerator, NotebookSlugGenerator>();

        return services;
    }
}
