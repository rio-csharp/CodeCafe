using CodeCafe.Api.Common;
using CodeCafe.Api.Configuration;
using CodeCafe.Api.Errors;
using CodeCafe.Mcp.Configuration;
using CodeCafe.Mcp.Tools.Diagnostics;
using CodeCafe.Server.Configuration;
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
        app.UseForwardedHeaders();
        app.UseCodeCafeCors();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseCodeCafeApiAntiforgery();
        app.UseAuthorization();
        app.UseCodeCafeMcpOriginValidation();
        app.MapCodeCafeApi();
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
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });

        return app;
    }

    private static IApplicationBuilder UseCodeCafeApiAntiforgery(this IApplicationBuilder app)
    {
        return app.Use(async (httpContext, next) =>
        {
            if (RequiresCsrfValidation(httpContext.Request))
            {
                var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();
                await antiforgery.ValidateRequestAsync(httpContext);
            }

            await next(httpContext);
        });
    }

    private static bool RequiresCsrfValidation(HttpRequest request)
    {
        if (!request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return HttpMethods.IsPost(request.Method)
            || HttpMethods.IsPut(request.Method)
            || HttpMethods.IsPatch(request.Method)
            || HttpMethods.IsDelete(request.Method);
    }
}
