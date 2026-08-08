// Placeholder entry point. The real composition root moves here once Domain, Application and
// Infrastructure have been populated; until then this only exists so the project compiles.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => Results.Ok(new { status = "scaffolding" }));
app.Run();
