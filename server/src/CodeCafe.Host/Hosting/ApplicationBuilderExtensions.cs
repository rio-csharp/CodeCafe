using Serilog;

namespace CodeCafe.Host.Hosting;

internal static class ApplicationBuilderExtensions
{
    public static WebApplication UseCodeCafePipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        // HSTS is omitted on purpose: Cloudflare terminates TLS at the edge and
        // already emits Strict-Transport-Security there.
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
