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
        // AuthOptions is a single boolean (RegistrationEnabled) with no invalid
        // state, so there is no meaningful startup validation rule to run.
        services.AddOptions<AuthOptions>()
            .BindConfiguration(AuthOptions.SectionName);

        return services;
    }
}
