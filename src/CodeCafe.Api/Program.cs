using CodeCafe.Api.Common;
using CodeCafe.Api.DependencyInjection;
using CodeCafe.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddCodeCafeApi();

var app = builder.Build();

app.UseExceptionHandler();
app.MapCodeCafeApi();

await app.RunAsync();

public partial class Program;
