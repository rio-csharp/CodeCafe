using CodeCafe.Api.Common;
using CodeCafe.Api.DependencyInjection;
using CodeCafe.Application;
using CodeCafe.Infrastructure;
using CodeCafe.Mcp.Common;
using CodeCafe.Mcp.DependencyInjection;
using CodeCafe.Server.Common;
using CodeCafe.Server.DependencyInjection;
using CodeCafe.Server.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddCodeCafeApi(builder.Environment);
builder.Services.AddCodeCafeMcp();
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

app.UseCodeCafeApiPipeline();
app.MapControllers();
app.MapCodeCafeMcpProtectedResourceMetadata();
app.MapCodeCafeMcp();

await app.RunAsync();

public partial class Program;
