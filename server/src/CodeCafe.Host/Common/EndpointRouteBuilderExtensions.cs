using CodeCafe.Host.Rest.Auth;
using CodeCafe.Host.Rest.Notes;

namespace CodeCafe.Host.Common;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCodeCafeApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAuthEndpoints();
        endpoints.MapHealthEndpoints();
        endpoints.MapNotesEndpoints();
        return endpoints;
    }
}
