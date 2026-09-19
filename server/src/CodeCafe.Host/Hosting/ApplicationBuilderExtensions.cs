using Serilog;

namespace CodeCafe.Host.Hosting;

internal static class ApplicationBuilderExtensions
{
    public static WebApplication UseCodeCafePipeline(this WebApplication app)
    {
        // Must precede anything that reads RemoteIpAddress (rate limiting, logging).
        app.UseCodeCafeForwardedHeaders();
        // Outside the exception handler so the logged status code is the one the client
        // actually received; the handler logs exception details itself.
        app.UseSerilogRequestLogging();
        app.UseExceptionHandler();
        // HSTS is omitted on purpose: Cloudflare terminates TLS at the edge and
        // already emits Strict-Transport-Security there.
        app.UseCodeCafeSecurityHeaders();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        return app;
    }
}
