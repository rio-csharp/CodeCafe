using CodeCafe.Infrastructure.Auth;
using CodeCafe.Infrastructure.Notes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCodeCafeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthInfrastructure(configuration);
        services.AddSingleton<INotesSettingsRepository>(_ =>
            new InMemoryNotesSettingsRepository(configuration["Notes:RootPath"] ?? string.Empty));
        services.AddScoped<INotesRepository, FileSystemNotesRepository>();

        return services;
    }
}
