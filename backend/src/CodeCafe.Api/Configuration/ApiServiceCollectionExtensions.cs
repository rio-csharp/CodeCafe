using CodeCafe.Api.Authentication;
using CodeCafe.Api.Authorization;
using CodeCafe.Api.HealthChecks;

namespace CodeCafe.Api.Configuration;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddCodeCafeApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddApiOptions(configuration)
            .AddApiPresentation()
            .AddApiForwardedHeaders()
            .AddApiAuthentication()
            .AddApiAuthorization()
            .AddApiRateLimiting()
            .AddApiCors(configuration)
            .AddApiHealthChecks();

        return services;
    }
}
