using CodeCafe.Api.Endpoints.Health;
using CodeCafe.Api.Endpoints.Notes;

namespace CodeCafe.Api.Common;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCodeCafeApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthEndpoints();
        endpoints.MapNotesEndpoints();
        return endpoints;
    }
}
