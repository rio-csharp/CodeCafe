using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CodeCafe.Host.Hosting;

// Tagged "ready" and never "live": draining has to stop traffic, not make the orchestrator kill
// the pod that is still finishing the requests it already accepted.
internal sealed class ServerDrainReadinessHealthCheck(ServerDrainState drainState) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    ) =>
        Task.FromResult(
            drainState.IsDraining
                ? HealthCheckResult.Unhealthy("The server is draining and is not accepting new traffic.")
                : HealthCheckResult.Healthy("The server is accepting new traffic.")
        );
}
