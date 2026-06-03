using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CodeCafe.Api.Endpoints.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/health");

        group.MapHealthChecks("/live", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live"),
            ResponseWriter = (httpContext, report) => WriteHealthResponseAsync(httpContext, report, healthyStatus: "ok")
        });

        group.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResponseWriter = (httpContext, report) => WriteHealthResponseAsync(httpContext, report, healthyStatus: "ready")
        });

        return endpoints;
    }

    private static Task WriteHealthResponseAsync(
        HttpContext httpContext,
        HealthReport report,
        string healthyStatus)
    {
        httpContext.Response.ContentType = "application/json";

        return httpContext.Response.WriteAsJsonAsync(new
        {
            status = report.Status switch
            {
                HealthStatus.Healthy => healthyStatus,
                HealthStatus.Degraded => "degraded",
                HealthStatus.Unhealthy => "unhealthy",
                _ => report.Status.ToString().ToLowerInvariant()
            },
            adapter = "api",
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString().ToLowerInvariant(),
                    description = entry.Value.Description
                })
        });
    }
}
