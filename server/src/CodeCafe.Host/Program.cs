using CodeCafe.Host.AgUi;
using CodeCafe.Application.Identity;
using CodeCafe.Infrastructure.Identity;
using CodeCafe.Host.OAuth;
using CodeCafe.Host.Mcp;
using CodeCafe.Application.Notes;
using CodeCafe.Infrastructure.Notes;
using CodeCafe.Host.Common;
using CodeCafe.Host.DependencyInjection;

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
