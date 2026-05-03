using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Application;

using CodeCafe.Application.Ai;

public static class DependencyInjection
{
    public static IServiceCollection AddCodeCafeApplication(this IServiceCollection services)
    {
        services.AddScoped<IAiProviderConfigurationService, AiProviderConfigurationService>();

        return services;
    }
}
