namespace CodeCafe.Api.Endpoints.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/health");

        group.MapGet("/live", () => Results.Ok(new
        {
            status = "ok",
            adapter = "api"
        }));

        return endpoints;
    }
}
