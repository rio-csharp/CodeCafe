using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CodeCafe.Host.Hosting;

internal static class HealthCheckExtensions
{
    public static IServiceCollection AddCodeCafeHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy("The host process is running."),
                tags: ["live", "ready"]);
        return services;
    }

    public static WebApplication MapCodeCafeHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapHealthChecks("/health/live", ByTag("live")).AllowAnonymous();
        app.MapHealthChecks("/health/ready", ByTag("ready")).AllowAnonymous();
        return app;
    }

    private static HealthCheckOptions ByTag(string tag)
        => new() { Predicate = registration => registration.Tags.Contains(tag) };
}
