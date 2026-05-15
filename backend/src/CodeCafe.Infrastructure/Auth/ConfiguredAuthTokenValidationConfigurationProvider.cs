using CodeCafe.Application.Auth;
using Microsoft.Extensions.Options;

namespace CodeCafe.Infrastructure.Auth;

public sealed class ConfiguredAuthTokenValidationConfigurationProvider(
    IOptions<ConfiguredLoginOptions> configuredLoginOptions)
    : IAuthTokenValidationConfigurationProvider
{
    public AuthTokenValidationConfiguration Get()
    {
        return new AuthTokenValidationConfiguration(
            configuredLoginOptions.Value.JwtSigningKey,
            TimeSpan.FromMinutes(1));
    }
}
