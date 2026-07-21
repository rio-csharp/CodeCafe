using CodeCafe.Modules.Identity.Presentation.Endpoints.Auth;
using CodeCafe.Modules.Notes.Presentation.Endpoints.Notes;
using CodeCafe.Server.Endpoints.Health;

namespace CodeCafe.Server.Common;

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
