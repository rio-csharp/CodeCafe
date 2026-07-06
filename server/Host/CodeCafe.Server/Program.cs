using CodeCafe.Ai.DependencyInjection;
using CodeCafe.Api.DependencyInjection;
using CodeCafe.Infrastructure;
using CodeCafe.Mcp.DependencyInjection;
using CodeCafe.Modules.Identity.Application;
using CodeCafe.Modules.Notes.Application;
using CodeCafe.Server.Common;
using CodeCafe.Server.DependencyInjection;
using CodeCafe.Server.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityApplication();
builder.Services.AddNotesApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddCodeCafeApi();
builder.Services.AddCodeCafeAi(builder.Configuration);
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
