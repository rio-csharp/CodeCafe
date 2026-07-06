using CodeCafe.Api.Configuration;

namespace CodeCafe.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodeCafeApi(
        this IServiceCollection services)
    {
        return services.AddCodeCafeAuthOptions();
    }

    private static IServiceCollection AddCodeCafeAuthOptions(this IServiceCollection services)
    {
        services.AddOptions<AuthOptions>()
            .BindConfiguration(AuthOptions.SectionName)
            .ValidateOnStart();

        return services;
    }
}
