using CodeCafe.Application.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Infrastructure.Auth;

internal static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddAuthInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<ConfiguredLoginOptions>()
            .Bind(configuration.GetSection(ConfiguredLoginOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Username), "Authentication username is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Password), "Authentication password is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.JwtSigningKey), "Authentication JWT signing key is required.")
            .Validate(options => options.JwtTokenLifetimeMinutes > 0, "Authentication JWT token lifetime must be greater than zero.")
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ICredentialValidator, ConfiguredCredentialValidator>();
        services.AddSingleton<IAuthTokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<IAuthTokenValidationConfigurationProvider, ConfiguredAuthTokenValidationConfigurationProvider>();

        return services;
    }
}
