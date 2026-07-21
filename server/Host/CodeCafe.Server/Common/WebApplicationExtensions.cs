using CodeCafe.Modules.Ai.Common;
using CodeCafe.Modules.Identity.Presentation.Configuration;
using CodeCafe.Modules.Mcp.Common;
using CodeCafe.Modules.Mcp.Tools.Diagnostics;
using CodeCafe.Modules.Notes.Presentation.Endpoints.Notes;
using CodeCafe.Modules.Notes.Presentation.Errors;
using CodeCafe.Shared.Application.Configuration;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;

namespace CodeCafe.Server.Common;

public static class WebApplicationExtensions
{
    public static WebApplication UseCodeCafeServerPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        if (app.Environment.IsProduction())
        {
            app.UseHsts();
        }
        app.UseForwardedHeaders();
        app.UseCodeCafeSecurityHeaders();
        app.UseCodeCafeCors();
        app.UseResponseCompression();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseCodeCafeApiAntiforgery();
        app.UseAuthorization();
        app.UseCodeCafeMcpOriginValidation();
        app.MapCodeCafeApi();
        app.MapCodeCafeAi();
        app.MapNotesMarkdownImportEndpoints();
        app.MapDiagnosticsToolEndpoints();
        app.MapControllers();
        app.MapCodeCafeMcpProtectedResourceMetadata();
        app.MapCodeCafeMcp();
        return app;
    }

    public static IEndpointRouteBuilder MapCodeCafeMcpProtectedResourceMetadata(this IEndpointRouteBuilder endpoints)
    {
        var mcpOptions = endpoints.ServiceProvider.GetRequiredService<IOptions<McpOptions>>().Value;
        var authorizationServerOptions = endpoints.ServiceProvider.GetRequiredService<IOptions<AuthorizationServerOptions>>().Value;
        if (!mcpOptions.Enabled || !mcpOptions.RequireAuthorization)
        {
            return endpoints;
        }

        endpoints.MapGet(mcpOptions.ProtectedResourceMetadataPath, (HttpContext httpContext) =>
        {
            var resource = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{mcpOptions.EndpointPath}";
            return Results.Ok(new
            {
                resource,
                authorization_servers = new[] { authorizationServerOptions.Issuer.TrimEnd('/') },
                scopes_supported = mcpOptions.RequiredReadScopes.Concat(mcpOptions.RequiredWriteScopes).Distinct().ToArray()
            });
        })
        .AllowAnonymous();

        return endpoints;
    }

    public static IApplicationBuilder UseCodeCafeMcpOriginValidation(this IApplicationBuilder app)
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
                && !originValues.Any(value => options.AllowedOrigins.Contains(value, StringComparer.OrdinalIgnoreCase)))
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                await Results.Problem(ApiProblems.Create(
                    "origin_forbidden",
                    "Origin is not allowed for MCP.",
                    StatusCodes.Status403Forbidden)).ExecuteAsync(httpContext);
                return;
            }

            await next(httpContext);

            if (httpContext.Response.StatusCode == StatusCodes.Status401Unauthorized && options.RequireAuthorization)
            {
                var resourceMetadataUri = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{options.ProtectedResourceMetadataPath}";
                httpContext.Response.Headers.Append(
                    "WWW-Authenticate",
                    $"Bearer resource_metadata=\"{resourceMetadataUri}\"");
            }
        });
    }

    public static IEndpointRouteBuilder MapCodeCafeMcp(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<McpOptions>>().Value;

        if (!options.Enabled)
        {
            return endpoints;
        }

        endpoints.MapMcpHttpUploadEndpoints();
        var endpoint = endpoints.MapMcp(options.EndpointPath);
        endpoint.RequireRateLimiting("mcp");
        if (options.RequireAuthorization)
        {
            endpoint.RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme
            });
        }

        return endpoints;
    }

    private static IApplicationBuilder UseCodeCafeSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (httpContext, next) =>
        {
            // Defense-in-depth for any HTML-ish response (error pages, OIDC
            // flows). Inert for JSON API responses.
            var headers = httpContext.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            await next(httpContext);
        });
    }

    private static IApplicationBuilder UseCodeCafeCors(this WebApplication app)
    {
        var corsOptions = app.Services.GetRequiredService<IOptions<CorsOptions>>().Value;
        if (corsOptions.AllowedOrigins.Length == 0)
        {
            return app;
        }

        app.UseCors(policy =>
        {
            policy.WithOrigins(corsOptions.AllowedOrigins)
                .WithHeaders("Content-Type", "X-CSRF-TOKEN", "Authorization")
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                .AllowCredentials();
        });

        return app;
    }

    private static IApplicationBuilder UseCodeCafeApiAntiforgery(this IApplicationBuilder app)
    {
        return app.Use(async (httpContext, next) =>
        {
            if (RequiresCsrfValidation(httpContext))
            {
                var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();
                await antiforgery.ValidateRequestAsync(httpContext);
            }

            await next(httpContext);
        });
    }

    private static bool RequiresCsrfValidation(HttpContext httpContext)
    {
        var request = httpContext.Request;
        if (!request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (request.Path.StartsWithSegments("/api/mcp", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var antiforgeryMetadata = httpContext.GetEndpoint()?.Metadata.GetMetadata<IAntiforgeryMetadata>();
        if (antiforgeryMetadata is not null && !antiforgeryMetadata.RequiresValidation)
        {
            return false;
        }

        return HttpMethods.IsPost(request.Method)
            || HttpMethods.IsPut(request.Method)
            || HttpMethods.IsPatch(request.Method)
            || HttpMethods.IsDelete(request.Method);
    }
}
