using CodeCafe.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace CodeCafe.UnitTests.Infrastructure.Auth;

public sealed class ConfiguredAuthTokenValidationConfigurationProviderTests
{
    [Fact]
    public void Get_returns_signing_key_and_clock_skew_for_token_validation()
    {
        var provider = new ConfiguredAuthTokenValidationConfigurationProvider(
            Options.Create(new ConfiguredLoginOptions
            {
                Username = "admin",
                Password = "secret",
                JwtSigningKey = "0123456789abcdef0123456789abcdef",
            }));

        var configuration = provider.Get();

        Assert.Equal("0123456789abcdef0123456789abcdef", configuration.SigningKey);
        Assert.Equal(TimeSpan.FromMinutes(1), configuration.ClockSkew);
    }
}
