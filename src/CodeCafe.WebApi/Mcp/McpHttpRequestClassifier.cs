using Microsoft.Extensions.Options;

namespace CodeCafe.WebApi.Mcp;

internal static class McpHttpRequestClassifier
{
    public static bool IsProtocolRequest(HttpContext httpContext, IOptions<McpOptions> optionsAccessor)
        => IsProtocolRequest(httpContext, optionsAccessor.Value);

    public static bool IsProtocolRequest(HttpContext httpContext, McpOptions options)
    {
        if (!options.Enabled)
        {
            return false;
        }

        return httpContext.Request.Path.StartsWithSegments(options.EndpointPath, StringComparison.OrdinalIgnoreCase);
    }
}
