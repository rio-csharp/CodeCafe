using CodeCafe.Api.Common;
using CodeCafe.Api.DependencyInjection;
using CodeCafe.Application;
using CodeCafe.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddCodeCafeApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapCodeCafeApi();

await app.RunAsync();

public partial class Program;
