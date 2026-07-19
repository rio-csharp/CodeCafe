using CodeCafe.Modules.Identity.Presentation.Configuration;

namespace CodeCafe.Modules.Identity.Presentation.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityPresentation(
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
