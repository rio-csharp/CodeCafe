using CodeCafe.Shared.Application.Configuration;
using CodeCafe.Modules.Mcp.Tools.Diagnostics;
using Microsoft.Extensions.Options;

namespace CodeCafe.Modules.Mcp.Common;

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
