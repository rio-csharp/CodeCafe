namespace CodeCafe.Api.Infrastructure;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class ReadinessHealthCheck : IHealthCheck
{
    private volatile bool isShuttingDown;

    public ReadinessHealthCheck(IHostApplicationLifetime applicationLifetime)
    {
        applicationLifetime.ApplicationStopping.Register(() => isShuttingDown = true);
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(isShuttingDown
            ? HealthCheckResult.Unhealthy("The application is shutting down.")
            : HealthCheckResult.Healthy());
    }
}
