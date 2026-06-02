using CodeCafe.Application;
using CodeCafe.Infrastructure;
using CodeCafe.Mcp.Common;
using CodeCafe.Mcp.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddCodeCafeMcp(builder.Configuration);

var app = builder.Build();

app.UseCodeCafeMcpPipeline();

await app.RunAsync();

public partial class Program;
