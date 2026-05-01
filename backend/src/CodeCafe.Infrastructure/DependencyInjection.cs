using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCodeCafeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;

        return services;
    }
}
