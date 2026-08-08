using CodeCafe.Modules.Ai.Common;
using CodeCafe.Application.Ai;
using CodeCafe.Application.Ai.Edits.Commands.ApplyNotebookEditProposal;
using CodeCafe.Application.Ai.Edits.Commands.CreateNotebookEditProposal;
using CodeCafe.Application.Ai.Edits.Commands.DiscardNotebookEditProposal;
using CodeCafe.Application.Ai.Edits.Queries.GetNotebookEditProposal;
using CodeCafe.Application.Common.Identity;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace CodeCafe.Application.Ai.Edits;

public static class AiNotebookEditEndpoints
{
    public static IEndpointRouteBuilder MapAiNotebookEditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<AiOptions>>().Value;
        if (!options.Enabled)
        {
            return endpoints;
        }

        var basePath = options.EditEndpointPath.TrimEnd('/');
        endpoints.MapPost(basePath, CreateNotebookEditProposalAsync)
            .RequireAuthorization()
            .RequireRateLimiting("ai");
        endpoints.MapGet(basePath + "/proposals/{proposalId:guid}", GetNotebookEditProposalAsync)
            .RequireAuthorization()
            .RequireRateLimiting("ai");
        endpoints.MapPost(basePath + "/proposals/{proposalId:guid}/apply", ApplyNotebookEditProposalAsync)
            .RequireAuthorization()
            .RequireRateLimiting("ai");
        endpoints.MapDelete(basePath + "/proposals/{proposalId:guid}", DiscardNotebookEditProposalAsync)
            .RequireAuthorization()
            .RequireRateLimiting("ai");

        return endpoints;
    }

    private static async Task<IResult> CreateNotebookEditProposalAsync(
        AiNotebookEditRequest request,
        ICurrentUserAccessor currentUserAccessor,
        ISender sender,
        IOptions<AiOptions> aiOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserAccessor.GetCurrentUserId() ?? Guid.Empty;
        if (actorId == Guid.Empty)
        {
            return AiProblemResults.ToError("authenticated_actor_required", "Authentication is required to generate notebook edits.", StatusCodes.Status401Unauthorized);
        }

        var result = await sender.Send(
            new CreateNotebookEditProposalCommand(
                actorId,
                request.NotebookSlug,
                request.ActivePagePath,
                request.Prompt,
                request.Operation,
                request.Locale,
                request.Apply,
                request.ParentPath,
                request.ExpectedUpdatedAtUtc),
            cancellationToken);
        if (!result.Succeeded)
        {
            return AiProblemResults.ToError(result.Error!);
        }

        return TypedResults.Ok(ToResponse(
            result.Proposal!,
            aiOptionsAccessor.Value.EditEndpointPath,
            result.Applied,
            result.SavedAtUtc));
    }

    private static async Task<IResult> GetNotebookEditProposalAsync(
        Guid proposalId,
        ICurrentUserAccessor currentUserAccessor,
        ISender sender,
        IOptions<AiOptions> aiOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserAccessor.GetCurrentUserId() ?? Guid.Empty;
        if (actorId == Guid.Empty)
        {
            return AiProblemResults.ToError("authenticated_actor_required", "Authentication is required to preview notebook edits.", StatusCodes.Status401Unauthorized);
        }

        var result = await sender.Send(
            new GetNotebookEditProposalQuery(proposalId, actorId),
            cancellationToken);
        if (!result.Succeeded)
        {
            return AiProblemResults.ToError(result.Error!);
        }

        return TypedResults.Ok(ToResponse(
            result.Proposal!,
            aiOptionsAccessor.Value.EditEndpointPath,
            applied: false,
            savedAtUtc: null));
    }

    private static async Task<IResult> ApplyNotebookEditProposalAsync(
        Guid proposalId,
        ICurrentUserAccessor currentUserAccessor,
        ISender sender,
        IOptions<AiOptions> aiOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserAccessor.GetCurrentUserId() ?? Guid.Empty;
        if (actorId == Guid.Empty)
        {
            return AiProblemResults.ToError("authenticated_actor_required", "Authentication is required to apply notebook edits.", StatusCodes.Status401Unauthorized);
        }

        var result = await sender.Send(
            new ApplyNotebookEditProposalCommand(proposalId, actorId),
            cancellationToken);
        if (!result.Succeeded)
        {
            return AiProblemResults.ToError(result.Error!);
        }

        return TypedResults.Ok(ToResponse(
            result.Proposal!,
            aiOptionsAccessor.Value.EditEndpointPath,
            result.Applied,
            result.SavedAtUtc));
    }

    private static async Task<IResult> DiscardNotebookEditProposalAsync(
        Guid proposalId,
        ICurrentUserAccessor currentUserAccessor,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserAccessor.GetCurrentUserId() ?? Guid.Empty;
        if (actorId == Guid.Empty)
        {
            return AiProblemResults.ToError("authenticated_actor_required", "Authentication is required to discard notebook edits.", StatusCodes.Status401Unauthorized);
        }

        var error = await sender.Send(
            new DiscardNotebookEditProposalCommand(proposalId, actorId),
            cancellationToken);
        if (error is not null)
        {
            return AiProblemResults.ToError(error);
        }

        return TypedResults.Ok(new { proposalId, discarded = true });
    }

    private static AiNotebookEditResponse ToResponse(
        AiNotebookEditProposal proposal,
        string editEndpointPath,
        bool applied,
        DateTimeOffset? savedAtUtc)
    {
        var basePath = editEndpointPath.TrimEnd('/');
        return new AiNotebookEditResponse(
            proposal.ProposalId,
            $"{basePath}/proposals/{proposal.ProposalId:D}",
            $"{basePath}/proposals/{proposal.ProposalId:D}/apply",
            $"{basePath}/proposals/{proposal.ProposalId:D}",
            proposal.ExpiresAtUtc,
            proposal.EffectiveOperation,
            proposal.Mode,
            applied,
            proposal.Summary,
            proposal.NotebookId,
            proposal.NotebookSlug,
            proposal.NotebookTitle,
            proposal.PageId,
            proposal.Title,
            proposal.PagePath,
            proposal.ParentPath,
            proposal.BeforeContentJson,
            proposal.BeforePlainTextContent,
            proposal.AfterContentJson,
            proposal.AfterPlainTextContent,
            proposal.OperationsJson,
            GetUtf8ByteCount(proposal.AfterContentJson.GetRawText()),
            proposal.AfterPlainTextContent?.Length ?? 0,
            CountTipTapNodes(proposal.AfterContentJson),
            proposal.GeneratedAtUtc,
            savedAtUtc);
    }

    private static int GetUtf8ByteCount(string? value)
        => string.IsNullOrEmpty(value) ? 0 : Encoding.UTF8.GetByteCount(value);

    private static int CountTipTapNodes(JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object)
        {
            return 0;
        }

        var count = 1;
        if (node.TryGetProperty("content", out var contentElement)
            && contentElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in contentElement.EnumerateArray())
            {
                count += CountTipTapNodes(child);
            }
        }

        return count;
    }
}

public sealed record AiNotebookEditRequest(
    string NotebookSlug,
    string? ActivePagePath,
    string Prompt,
    string? Operation,
    string? Locale,
    bool Apply = false,
    string? ParentPath = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);
