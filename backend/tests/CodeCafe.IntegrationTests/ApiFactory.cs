using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CodeCafe.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public WebApplicationFactory<Program> WithEnvironment(string environmentName)
    {
        return WithWebHostBuilder(builder => builder.UseEnvironment(environmentName));
    }
}
