using CodeCafe.Modules.Ai.Common;
using CodeCafe.Modules.Ai.Configuration;
using CodeCafe.Modules.Ai.Drafts.Commands.GenerateNoteDraft;
using CodeCafe.Shared.Application.Identity;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeCafe.Modules.Ai.Drafts;

public static class AiNoteDraftEndpoints
{
    public static IEndpointRouteBuilder MapAiNoteDraftEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<AiOptions>>().Value;
        if (!options.Enabled)
        {
            return endpoints;
        }

        endpoints.MapPost(options.DraftEndpointPath, GenerateDraftAsync)
            .RequireAuthorization()
            .RequireRateLimiting("ai");

        return endpoints;
    }

    private static async Task<IResult> GenerateDraftAsync(
        AiNoteDraftRequest request,
        ICurrentUserAccessor currentUserAccessor,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserAccessor.GetCurrentUserId() ?? Guid.Empty;
        if (actorId == Guid.Empty)
        {
            return AiHelpers.ToError("authenticated_actor_required", "Authentication is required to generate note drafts.", StatusCodes.Status401Unauthorized);
        }

        var result = await sender.Send(
            new GenerateNoteDraftCommand(
                actorId,
                request.NotebookSlug,
                request.ActivePagePath,
                request.Intent,
                request.Prompt,
                request.Locale),
            cancellationToken);
        if (!result.Succeeded)
        {
            return AiHelpers.ToError(result.Error!);
        }

        var draft = result.Draft!;
        return TypedResults.Ok(new AiNoteDraftResponse(
            draft.Markdown,
            draft.Title,
            draft.Intent,
            draft.NotebookSlug,
            draft.PagePath,
            DateTimeOffset.UtcNow));
    }
}

public sealed record AiNoteDraftRequest(
    string NotebookSlug,
    string? ActivePagePath,
    string? Intent,
    string Prompt,
    string? Locale);

public sealed record AiNoteDraftResponse(
    string Markdown,
    string Title,
    string Intent,
    string NotebookSlug,
    string? PagePath,
    DateTimeOffset GeneratedAtUtc);
