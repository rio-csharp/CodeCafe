using CodeCafe.Infrastructure.Auth;

namespace CodeCafe.Api.Authentication;

internal static class AuthenticationOptionsServiceCollectionExtensions
{
    public static IServiceCollection AddApiOptions(
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

        return services;
    }
}
