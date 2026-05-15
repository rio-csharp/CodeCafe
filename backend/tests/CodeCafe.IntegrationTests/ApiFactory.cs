using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CodeCafe.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:JwtSigningKey"] = "testing-jwt-signing-key-with-32-chars",
                ["Authentication:JwtTokenLifetimeMinutes"] = "4320",
                ["Authentication:Username"] = "test-user",
                ["Authentication:Password"] = "test-password",
            });
        });
    }

    public WebApplicationFactory<Program> WithEnvironment(string environmentName)
    {
        return WithWebHostBuilder(builder => builder.UseEnvironment(environmentName));
    }
}
