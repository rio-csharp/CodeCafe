using CodeCafe.Modules.Ai.Common;
using CodeCafe.Modules.Identity.Presentation.Configuration;
using CodeCafe.Modules.Mcp.Common;
using CodeCafe.Modules.Mcp.Tools.Diagnostics;
using CodeCafe.Modules.Notes.Presentation.Endpoints.Notes;
using CodeCafe.Server.Configuration;
using CodeCafe.Shared.Application.Configuration;
using CodeCafe.Shared.Presentation.Errors;
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
        LogForwardedHeadersConfigurationWarnings(app);
        app.UseCodeCafeSecurityHeaders();
        app.UseCodeCafeCors();
        app.UseResponseCompression();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseCodeCafeApiAntiforgery();
        app.UseCodeCafeMcpOriginValidation();
        app.UseAuthorization();
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

            if (options.RequireAuthorization)
            {
                var resourceMetadataUri = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{options.ProtectedResourceMetadataPath}";
                httpContext.Response.OnStarting(() =>
                {
                    if (httpContext.Response.StatusCode == StatusCodes.Status401Unauthorized)
                    {
                        EnsureMcpResourceMetadataChallenge(httpContext.Response, resourceMetadataUri);
                    }

                    return Task.CompletedTask;
                });
            }

            await next(httpContext);
        });
    }

    private static void EnsureMcpResourceMetadataChallenge(HttpResponse response, string resourceMetadataUri)
    {
        var challenges = response.Headers.WWWAuthenticate
            .Where(challenge => !string.IsNullOrWhiteSpace(challenge))
            .Select(challenge => challenge!)
            .ToList();
        if (challenges.Any(challenge => challenge!.Contains("resource_metadata=", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var metadataParameter = $"resource_metadata=\"{resourceMetadataUri}\"";
        var bearerIndex = challenges.FindIndex(challenge =>
            challenge.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
            || challenge.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase));
        if (bearerIndex >= 0)
        {
            challenges[bearerIndex] = challenges[bearerIndex].Equals("Bearer", StringComparison.OrdinalIgnoreCase)
                ? $"Bearer {metadataParameter}"
                : $"{challenges[bearerIndex]}, {metadataParameter}";
        }
        else
        {
            challenges.Add($"Bearer {metadataParameter}");
        }

        response.Headers.WWWAuthenticate = challenges.ToArray();
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

    private static void LogForwardedHeadersConfigurationWarnings(WebApplication app)
    {
        if (!app.Environment.IsProduction())
        {
            return;
        }

        var settings = app.Services.GetRequiredService<IOptions<ForwardedHeadersSettings>>().Value;
        var trustsOnlyLoopback = settings.KnownNetworks.Length > 0
            && settings.KnownNetworks.All(IsLoopbackNetwork);
        if (settings.KnownNetworks.Length == 0 || trustsOnlyLoopback)
        {
            // With no real proxy network trusted, X-Forwarded-For is ignored and
            // every request gets the ingress IP as RemoteIpAddress — collapsing
            // all IP-partitioned rate limits into a single shared partition.
            app.Logger.LogWarning(
                "ForwardedHeaders:KnownNetworks trusts no proxy network (loopback only) in Production. " +
                "Client IPs will resolve to the ingress address, collapsing IP-partitioned rate limits into one shared partition. " +
                "Configure the egress CIDR ranges of your reverse proxy/ingress.");
        }
    }

    private static bool IsLoopbackNetwork(string network)
    {
        return System.Net.IPNetwork.TryParse(network, out var parsed)
            && System.Net.IPAddress.IsLoopback(parsed.BaseAddress);
    }

    private static bool RequiresCsrfValidation(HttpContext httpContext)
    {
        var request = httpContext.Request;
        if (!request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // The MCP HTTP surface is also exposed under /api (e.g. the upload
        // endpoints) and authenticates with bearer tokens, not cookies, so CSRF
        // validation does not apply. Derive the exclusion from the configured
        // MCP endpoint path instead of hardcoding it.
        var mcpOptions = httpContext.RequestServices.GetRequiredService<IOptions<McpOptions>>().Value;
        if (request.Path.StartsWithSegments(GetMcpApiExclusionPath(mcpOptions), StringComparison.OrdinalIgnoreCase))
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

    private static string GetMcpApiExclusionPath(McpOptions mcpOptions)
    {
        var endpointPath = mcpOptions.EndpointPath;
        if (!endpointPath.StartsWith('/'))
        {
            endpointPath = "/" + endpointPath;
        }
        endpointPath = endpointPath.TrimEnd('/');
        if (endpointPath.Length == 0)
        {
            endpointPath = "/";
        }

        // Preserve the historical "/api/mcp" exclusion for the default "/mcp"
        // endpoint without doubling the prefix when the configured endpoint
        // already lives under /api.
        return endpointPath.Equals("/api", StringComparison.OrdinalIgnoreCase)
            || endpointPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
                ? endpointPath
                : "/api" + endpointPath;
    }
}
