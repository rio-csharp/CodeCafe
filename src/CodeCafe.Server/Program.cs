using CodeCafe.Api.DependencyInjection;
using CodeCafe.Application;
using CodeCafe.Infrastructure;
using CodeCafe.Mcp.DependencyInjection;
using CodeCafe.Server.Common;
using CodeCafe.Server.DependencyInjection;
using CodeCafe.Server.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddCodeCafeApi();
builder.Services.AddCodeCafeMcp(builder.Configuration);
builder.Services.AddCodeCafeServerHost(builder.Configuration, builder.Environment);

var app = builder.Build();

if (args is ["migrate", ..] or ["--migrate", ..])
{
    await app.Services.GetRequiredService<DatabaseMigrationRunner>().RunAsync(CancellationToken.None);
    return;
}

if (app.Environment.IsDevelopment())
{
    await app.Services.GetRequiredService<DatabaseMigrationRunner>().RunAsync(CancellationToken.None);
}

app.UseCodeCafeServerPipeline();

await app.RunAsync();

public partial class Program;
