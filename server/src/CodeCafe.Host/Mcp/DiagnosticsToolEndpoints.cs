namespace CodeCafe.Host.Mcp;
using CodeCafe.Application.Notes;

public static class DiagnosticsToolEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticsToolEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/mcp/health/live", () => Results.Ok(new
        {
            status = "ok",
            adapter = "mcp"
        }));

        return endpoints;
    }
}
