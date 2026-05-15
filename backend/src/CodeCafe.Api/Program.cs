using CodeCafe.Api.Configuration;
using CodeCafe.Application;
using CodeCafe.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddCodeCafeApplication();
builder.Services.AddCodeCafeInfrastructure(builder.Configuration);
builder.Services.AddCodeCafeApi(builder.Configuration);

var app = builder.Build();

app.UseApiPipeline();
app.MapApiEndpoints();

app.Run();

public partial class Program;
