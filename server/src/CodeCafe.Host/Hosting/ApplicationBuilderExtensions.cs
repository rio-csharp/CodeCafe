using Serilog;

namespace CodeCafe.Host.Hosting;

internal static class ApplicationBuilderExtensions
{
    public static WebApplication UseCodeCafePipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        // HSTS is managed at the Cloudflare edge (where TLS terminates); emitting it
        // here too would send duplicate Strict-Transport-Security headers.
        // if (app.Environment.IsProduction())
        //     app.UseHsts();
        // Must precede anything that reads RemoteIpAddress (rate limiting, logging).
        app.UseCodeCafeForwardedHeaders();
        app.UseSerilogRequestLogging();
        app.UseCodeCafeSecurityHeaders();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        return app;
    }
}
