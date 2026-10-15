using CodeCafe.Host.Hosting;
using CodeCafe.Host.Mcp;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseCodeCafeSerilog();
builder.Services.AddCodeCafe(builder.Configuration, builder.Environment);

var app = builder.Build();

// `migrate` applies pending migrations and exits without starting the HTTP server. Kubernetes
// runs it as a pre-upgrade hook Job, and the replicas it deploys behind set
// Database:MigrateOnStartup=false, so a rollout never has two pods racing inside MigrateAsync.
if (args.Contains("migrate", StringComparer.Ordinal))
{
    await app.ApplyMigrationsAsync(ignoreStartupSetting: true);
    return;
}

await app.ApplyMigrationsAsync();

app.UseCodeCafePipeline();
app.MapControllers();
app.MapApiDocumentation();
app.MapCodeCafeHealthChecks();
app.MapCodeCafeMcp();

app.Run();

// Public so WebApplicationFactory<Program> can bootstrap the app in tests.
public partial class Program;
