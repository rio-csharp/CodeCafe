namespace CodeCafe.Api.HealthChecks;

internal static class HealthCheckServiceCollectionExtensions
{
    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["ready"])
            .AddCheck<ReadinessHealthCheck>("readiness", tags: ["ready"]);

        return services;
    }
}
