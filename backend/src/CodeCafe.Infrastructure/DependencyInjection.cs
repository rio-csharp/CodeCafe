using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Infrastructure;

using CodeCafe.Application.Ai;
using CodeCafe.Infrastructure.Ai;

public static class DependencyInjection
{
    public static IServiceCollection AddCodeCafeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;

        services.AddSingleton<IAiProviderRepository, InMemoryAiProviderRepository>();

        return services;
    }
}
