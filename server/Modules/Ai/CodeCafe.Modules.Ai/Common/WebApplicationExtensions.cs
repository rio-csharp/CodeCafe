using CodeCafe.Application.Ai;
using CodeCafe.Application.Ai.Drafts;
using CodeCafe.Application.Ai.Edits;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeCafe.Application.Ai;

public static class WebApplicationExtensions
{
    public static IEndpointRouteBuilder MapCodeCafeAi(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<AiOptions>>().Value;
        endpoints.MapGet(options.StatusEndpointPath, () => Results.Ok(new AiStatusResponse(
                options.Enabled,
                options.Enabled ? options.EndpointPath : null,
                options.Enabled ? options.EditEndpointPath : null,
                options.Enabled ? options.DraftEndpointPath : null)))
            .AllowAnonymous();

        if (!options.Enabled)
        {
            return endpoints;
        }

        endpoints.MapAiNotebookEditEndpoints();
        endpoints.MapAiNoteDraftEndpoints();

        endpoints.MapAGUI(AiHelpers.NormalizeAgentName(options.AgentName), options.EndpointPath)
            .RequireAuthorization()
            .RequireRateLimiting("ai");

        return endpoints;
    }

    private sealed record AiStatusResponse(
        bool Enabled,
        string? EndpointPath,
        string? EditEndpointPath,
        string? DraftEndpointPath);
}
