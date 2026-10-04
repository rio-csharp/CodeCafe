using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CodeCafe.Host.Tests;

// Boots the app without a real database: startup migrations are disabled, so endpoint tests
// stay hermetic (paths that hit the database are covered by Infrastructure.Tests instead).
public sealed class CodeCafeFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:MigrateOnStartup"] = "false",
            }));
    }
}
