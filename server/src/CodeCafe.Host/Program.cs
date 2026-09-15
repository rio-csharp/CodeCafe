using CodeCafe.Host.Hosting;
using CodeCafe.Host.Mcp;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseCodeCafeSerilog();
builder.Services.AddCodeCafe(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseCodeCafePipeline();
app.MapControllers();
app.MapApiDocumentation();
app.MapCodeCafeHealthChecks();
app.MapCodeCafeMcp();

app.Run();

// Public so WebApplicationFactory<Program> can bootstrap the app in tests.
public partial class Program;
