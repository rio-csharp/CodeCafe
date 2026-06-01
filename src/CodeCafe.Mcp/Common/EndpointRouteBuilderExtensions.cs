using CodeCafe.Mcp.Tools.Diagnostics;

namespace CodeCafe.Mcp.Common;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCodeCafeMcp(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDiagnosticsToolEndpoints();
        return endpoints;
    }
}
