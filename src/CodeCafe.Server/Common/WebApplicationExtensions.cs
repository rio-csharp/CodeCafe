using CodeCafe.Api.Common;
using CodeCafe.Mcp.Tools.Diagnostics;
using CodeCafe.Server.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;

namespace CodeCafe.Server.Common;

public static class WebApplicationExtensions
{
    public static WebApplication UseCodeCafeServerPipeline(this WebApplication app)
    {
        app.UseCodeCafeApiPipeline();
        app.UseCodeCafeMcpOriginValidation();
        app.MapDiagnosticsToolEndpoints();
        app.MapControllers();
        app.MapCodeCafeMcpProtectedResourceMetadata();
        app.MapCodeCafeMcp();
        return app;
    }

    public static IEndpointRouteBuilder MapCodeCafeMcpProtectedResourceMetadata(this IEndpointRouteBuilder endpoints)
    {
        var mcpOptions = endpoints.ServiceProvider.GetRequiredService<IOptions<McpServerOptions>>().Value;
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
            var options = httpContext.RequestServices.GetRequiredService<IOptions<McpServerOptions>>().Value;
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
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    code = "origin_forbidden",
                    message = "Origin is not allowed for MCP."
                });
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
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<McpServerOptions>>().Value;

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
}
