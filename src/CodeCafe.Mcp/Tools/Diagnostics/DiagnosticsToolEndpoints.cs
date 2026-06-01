namespace CodeCafe.Mcp.Tools.Diagnostics;

public static class DiagnosticsToolEndpoints
{
    public static IEndpointRouteBuilder MapDiagnosticsToolEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok(new
        {
            status = "ok",
            adapter = "mcp"
        }));

        return endpoints;
    }
}
