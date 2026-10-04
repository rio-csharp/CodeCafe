using CodeCafe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CodeCafe.Host.Hosting;

// Readiness gate: the app only accepts traffic when PostgreSQL answers. Deliberately tagged
// "ready" and not "live" — a database outage must not make the orchestrator kill the pod,
// only stop routing to it.
internal sealed class PostgresHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Cannot connect to PostgreSQL.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Cannot connect to PostgreSQL.", exception);
        }
    }
}
