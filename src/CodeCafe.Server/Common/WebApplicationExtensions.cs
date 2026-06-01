using CodeCafe.Server.Configuration;
using Microsoft.Extensions.Options;

namespace CodeCafe.Server.Common;

public static class WebApplicationExtensions
{
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
}
