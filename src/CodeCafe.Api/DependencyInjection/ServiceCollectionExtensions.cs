using CodeCafe.Api.Configuration;
using CodeCafe.Api.Endpoints.Auth;

namespace CodeCafe.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodeCafeApi(
        this IServiceCollection services)
    {
        services.AddCodeCafeAuthOptions();
        services.AddCodeCafeAuthEndpointService();
        return services;
    }

    private static IServiceCollection AddCodeCafeAuthOptions(this IServiceCollection services)
    {
        services.AddOptions<AuthOptions>()
            .BindConfiguration(AuthOptions.SectionName)
            .ValidateOnStart();

        return services;
    }

    private static IServiceCollection AddCodeCafeAuthEndpointService(this IServiceCollection services)
    {
        services.AddScoped<IAuthEndpointService, IdentityAuthEndpointService>();
        return services;
    }
}
