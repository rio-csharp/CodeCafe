using CodeCafe.Api.Common;
using CodeCafe.Api.DependencyInjection;
using CodeCafe.Application;
using CodeCafe.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddCodeCafeApi(builder.Configuration, builder.Environment);
builder.Services.AddCodeCafeForwardedHeaders();

var app = builder.Build();

app.UseCodeCafeApiPipeline();

await app.RunAsync();

public partial class Program;
