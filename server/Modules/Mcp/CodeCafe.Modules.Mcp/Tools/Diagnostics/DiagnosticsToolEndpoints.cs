namespace CodeCafe.Modules.Mcp.Tools.Diagnostics;

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
