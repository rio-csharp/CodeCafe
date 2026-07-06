using CodeCafe.Mcp.Configuration;
using CodeCafe.Mcp.Tools.Diagnostics;
using Microsoft.Extensions.Options;

namespace CodeCafe.Mcp.Common;

public static class WebApplicationExtensions
{
    public static IEndpointRouteBuilder MapCodeCafeMcp(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<McpOptions>>().Value;

        endpoints.MapDiagnosticsToolEndpoints();
        endpoints.MapMcpHttpUploadEndpoints();

        if (options.Enabled)
        {
            endpoints.MapMcp(options.EndpointPath);
        }

        return endpoints;
    }
}
