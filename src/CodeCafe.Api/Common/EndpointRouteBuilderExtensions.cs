using CodeCafe.Api.Endpoints.Health;

namespace CodeCafe.Api.Common;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCodeCafeApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthEndpoints();
        return endpoints;
    }
}
