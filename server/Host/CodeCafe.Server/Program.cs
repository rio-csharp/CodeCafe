using CodeCafe.Modules.Ai.DependencyInjection;
using CodeCafe.Application.Identity;
using CodeCafe.Modules.Identity.Infrastructure;
using CodeCafe.Modules.Identity.Presentation.DependencyInjection;
using CodeCafe.Modules.Mcp.DependencyInjection;
using CodeCafe.Application.Notes;
using CodeCafe.Modules.Notes.Infrastructure;
using CodeCafe.Server.Common;
using CodeCafe.Server.DependencyInjection;
using CodeCafe.Server.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure();
builder.Services.AddNotesApplication();
builder.Services.AddNotesInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddIdentityPresentation();
builder.Services.AddCodeCafeAi(builder.Configuration, builder.Environment);
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
    await app.Services.GetRequiredService<DatabaseMigrationRunner>().RunAsync(app.Lifetime.ApplicationStopping);
}

app.UseCodeCafeServerPipeline();

await app.RunAsync();

public partial class Program;
