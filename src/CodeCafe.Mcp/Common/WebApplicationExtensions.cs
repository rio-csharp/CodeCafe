using CodeCafe.Mcp.Configuration;
using CodeCafe.Mcp.Tools.Diagnostics;
using Microsoft.Extensions.Options;

namespace CodeCafe.Mcp.Common;

public static class WebApplicationExtensions
{
    public static WebApplication UseCodeCafeMcpPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.MapCodeCafeMcp();
        return app;
    }

    public static IEndpointRouteBuilder MapCodeCafeMcp(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<McpOptions>>().Value;

        endpoints.MapDiagnosticsToolEndpoints();

        if (options.Enabled)
        {
            endpoints.MapMcp(options.EndpointPath);
        }

        return endpoints;
    }
}
