using CodeCafe.Application;
using CodeCafe.Infrastructure;
using CodeCafe.WebApi.Extensions;
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
    app.UseCodeCafePipeline();
    await app.RunAsync();
}
catch (Exception exception) when (exception.GetType().Name != "HostAbortedException")
{
    Log.Fatal(exception, "CodeCafe API terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
