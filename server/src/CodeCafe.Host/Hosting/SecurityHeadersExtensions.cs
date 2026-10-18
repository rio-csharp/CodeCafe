namespace CodeCafe.Host.Hosting;

internal static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseCodeCafeSecurityHeaders(this IApplicationBuilder app)
        => app.Use(async (context, next) =>
        {
            // Defense-in-depth for HTML-ish responses; inert for JSON. Scalar's UI pulls
            // assets from a CDN and inline scripts, so a strict CSP would break it.
            if (!context.Request.Path.StartsWithSegments("/scalar"))
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["Referrer-Policy"] = "no-referrer";
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            }

            // API reads carry per-user state (favorites, shares, page bodies). A shared proxy or
            // CDN must never replay one caller's response to another.
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.Headers.CacheControl = "no-store";
            }

            await next(context);
        });
}
