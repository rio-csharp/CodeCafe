using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CodeCafe.Host.Common;

public sealed class ServerDrainReadinessHealthCheck(ServerDrainState drainState) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(
            drainState.IsDraining
                ? HealthCheckResult.Unhealthy("Server is draining and not accepting new traffic.")
                : HealthCheckResult.Healthy("Server is accepting new traffic.")
        );
    }
}
