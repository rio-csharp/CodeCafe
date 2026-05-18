using CodeCafe.Application;
using CodeCafe.Infrastructure;
using CodeCafe.WebApi.Extensions;
using CodeCafe.WebApi.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddCodeCafeSerilog();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddWebApiServices(builder.Configuration, builder.Environment);
builder.Services.AddCodeCafeForwardedHeaders();

try
{
    var app = builder.Build();
    if (args is ["migrate", ..] or ["--migrate", ..])
    {
        await app.Services.GetRequiredService<DatabaseMigrationRunner>().RunAsync(CancellationToken.None);
        return;
    }

    app.UseCodeCafePipeline();
    await app.RunAsync();
}
catch (Exception exception) when (
    !builder.Environment.IsEnvironment("Testing")
    && exception.GetType().Name != "HostAbortedException")
{
    Log.Fatal(exception, "CodeCafe API terminated unexpectedly.");
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
