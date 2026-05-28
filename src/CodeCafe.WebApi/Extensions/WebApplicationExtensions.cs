using CodeCafe.WebApi.Health;
using CodeCafe.WebApi.Auth;
using CodeCafe.WebApi.Mcp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using Serilog;

namespace CodeCafe.WebApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseCodeCafePipeline(this WebApplication app)
    {
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseSerilogRequestLogging();
        app.UseForwardedHeaders();

        // Production HTTPS is enforced at the edge/ingress layer.
        if (!app.Environment.IsProduction())
        {
            app.UseHttpsRedirection();
        }

        app.UseCodeCafeCors();
        app.UseCodeCafeMcpOriginValidation();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseCodeCafeAntiforgery();
        app.UseAuthorization();

        app.MapHealthEndpoints();
        app.MapCodeCafeCsrfEndpoint();
        app.MapCodeCafeMcpProtectedResourceMetadata();
        app.MapControllers();
        app.MapCodeCafeMcpEndpoint();

        return app;
    }

    private static IEndpointRouteBuilder MapCodeCafeMcpEndpoint(this IEndpointRouteBuilder app)
    {
        var options = app.ServiceProvider.GetRequiredService<IOptions<McpOptions>>().Value;
        if (!options.Enabled)
        {
            return app;
        }

        var endpoint = app.MapMcp(options.EndpointPath);
        endpoint.RequireRateLimiting("mcp");
        if (options.RequireAuthorization)
        {
            endpoint.RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme
            });
        }

        return app;
    }

    private static IEndpointRouteBuilder MapCodeCafeMcpProtectedResourceMetadata(this IEndpointRouteBuilder app)
    {
        var options = app.ServiceProvider.GetRequiredService<IOptions<McpOptions>>().Value;
        var authorizationServerOptions = app.ServiceProvider.GetRequiredService<IOptions<AuthorizationServerOptions>>().Value;
        if (!options.Enabled || !options.RequireAuthorization)
        {
            return app;
        }

        app.MapGet(options.ProtectedResourceMetadataPath, (HttpContext httpContext) =>
        {
            var resource = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{options.EndpointPath}";
            return Results.Ok(new
            {
                resource,
                authorization_servers = new[] { authorizationServerOptions.Issuer.TrimEnd('/') },
                scopes_supported = options.RequiredReadScopes.Concat(options.RequiredWriteScopes).Distinct().ToArray()
            });
        })
        .AllowAnonymous();

        return app;
    }

    private static IApplicationBuilder UseCodeCafeMcpOriginValidation(this IApplicationBuilder app)
    {
        return app.Use(async (httpContext, next) =>
        {
            var options = httpContext.RequestServices.GetRequiredService<IOptions<McpOptions>>().Value;
            var isMcpRequest = options.Enabled
                && (httpContext.Request.Path.StartsWithSegments(options.EndpointPath, StringComparison.OrdinalIgnoreCase)
                    || httpContext.Request.Path.StartsWithSegments(options.ProtectedResourceMetadataPath, StringComparison.OrdinalIgnoreCase));

            if (!isMcpRequest)
            {
                await next(httpContext);
                return;
            }

            if (httpContext.Request.Headers.TryGetValue("Origin", out var originValues)
                && originValues.Count > 0
                && options.AllowedOrigins.Length > 0
                && !options.AllowedOrigins.Contains(originValues[0], StringComparer.OrdinalIgnoreCase))
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                await httpContext.Response.WriteAsJsonAsync(new { code = "origin_forbidden", message = "Origin is not allowed for MCP." });
                return;
            }

            await next(httpContext);

            if (httpContext.Response.StatusCode == StatusCodes.Status401Unauthorized)
            {
                var resourceMetadataUri = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{options.ProtectedResourceMetadataPath}";
                httpContext.Response.Headers.Append(
                    "WWW-Authenticate",
                    $"Bearer resource_metadata=\"{resourceMetadataUri}\"");
            }
        });
    }

    private static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health/live", () => Results.Ok(new { status = "Live" }));
        app.MapGet("/health/ready", (ReadinessState readinessState) =>
        {
            return readinessState.IsReady
                ? Results.Ok(new { status = "Ready" })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        });
    }
}
