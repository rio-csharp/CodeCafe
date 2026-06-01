using CodeCafe.Mcp.Common;
using CodeCafe.Application;
using CodeCafe.Mcp.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddCodeCafeMcp();

var app = builder.Build();

app.UseExceptionHandler();
app.MapCodeCafeMcp();

await app.RunAsync();

public partial class Program;
