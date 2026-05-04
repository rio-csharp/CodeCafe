using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Infrastructure;

using CodeCafe.Application.Notes;
using CodeCafe.Infrastructure.Notes;

public static class DependencyInjection
{
    public static IServiceCollection AddCodeCafeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<INotesSettingsRepository>(_ =>
            new InMemoryNotesSettingsRepository(configuration["Notes:RootPath"] ?? string.Empty));
        services.AddScoped<INotesRepository, FileSystemNotesRepository>();

        return services;
    }
}
