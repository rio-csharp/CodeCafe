using CodeCafe.Application.Notes;
using CodeCafe.Infrastructure.Notes.Read;
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
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        services.AddSingleton<INotebookAccessCodeHasher, NotebookAccessCodeHasher>();
        services.AddScoped<INotebookReadService, NotebookReadService>();

        return services;
    }
}
