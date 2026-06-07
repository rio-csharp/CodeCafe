using CodeCafe.Ai.Configuration;
using CodeCafe.Application.Notes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace CodeCafe.Ai.Drafts;

public static class AiNoteDraftEndpoints
{
    private static readonly string[] SupportedIntents =
    [
        "summarize",
        "outline",
        "rewrite",
        "expand",
        "continue",
        "custom"
    ];

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
        HttpContext httpContext,
        INotebookReadService notebookReadService,
        IAiNoteDraftGenerator draftGenerator,
        CancellationToken cancellationToken)
    {
        var actorId = GetCurrentUserId(httpContext.User);
        if (actorId == Guid.Empty)
        {
            return ToError("authenticated_actor_required", "Authentication is required to generate note drafts.", StatusCodes.Status401Unauthorized);
        }

        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return validationError;
        }

        var notebookResult = await notebookReadService.GetNotebookBySlugAsync(
            request.NotebookSlug.Trim(),
            actorId,
            cancellationToken,
            includeArchived: false,
            includeItems: true);
        if (!notebookResult.Succeeded)
        {
            return ToNotesError(notebookResult.Error!);
        }

        var activePage = ResolveActivePage(notebookResult.Value!, request.ActivePagePath);
        if (request.ActivePagePath is not null && activePage is null)
        {
            return ToError("notebook_item_not_found", "Notebook item was not found.", StatusCodes.Status404NotFound, "activePagePath");
        }

        var normalizedIntent = NormalizeIntent(request.Intent);
        AiNoteDraftResult result;
        try
        {
            result = await draftGenerator.GenerateDraftAsync(
                new AiNoteDraftGenerationContext(
                    actorId,
                    normalizedIntent,
                    request.Prompt.Trim(),
                    NormalizeLocale(request.Locale),
                    notebookResult.Value!,
                    activePage),
                cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ToError(
                "ai_draft_generation_failed",
                "The assistant could not generate a note draft. Please try again.",
                StatusCodes.Status502BadGateway);
        }

        var markdown = result.Markdown.Trim();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return ToError("empty_ai_draft", "The assistant returned an empty draft.", StatusCodes.Status502BadGateway);
        }

        var title = ExtractTitle(markdown)
            ?? activePage?.Title
            ?? $"{notebookResult.Value!.Title} AI draft";

        return TypedResults.Ok(new AiNoteDraftResponse(
            markdown,
            title,
            normalizedIntent,
            notebookResult.Value!.Slug,
            activePage?.Path,
            DateTimeOffset.UtcNow));
    }

    private static IResult? ValidateRequest(AiNoteDraftRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NotebookSlug))
        {
            return ToError("invalid_notebook_slug", "Notebook slug is required.", StatusCodes.Status400BadRequest, "notebookSlug");
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return ToError("invalid_prompt", "Prompt is required.", StatusCodes.Status400BadRequest, "prompt");
        }

        return null;
    }

    private static NotebookItemModel? ResolveActivePage(NotebookDetailModel notebook, string? activePagePath)
    {
        if (string.IsNullOrWhiteSpace(activePagePath))
        {
            return null;
        }

        var normalizedPath = NotebookInput.NormalizePath(activePagePath);
        return notebook.Items.SingleOrDefault(item =>
            string.Equals(item.Path, normalizedPath, StringComparison.Ordinal)
            && string.Equals(item.Type, "page", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeIntent(string? intent)
    {
        var normalized = string.IsNullOrWhiteSpace(intent)
            ? "custom"
            : intent.Trim().ToLowerInvariant();

        return SupportedIntents.Contains(normalized, StringComparer.Ordinal)
            ? normalized
            : "custom";
    }

    private static string NormalizeLocale(string? locale)
    {
        return string.IsNullOrWhiteSpace(locale)
            ? "en"
            : locale.Trim();
    }

    private static string? ExtractTitle(string markdown)
    {
        foreach (var line in markdown.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                var title = trimmed[2..].Trim();
                return string.IsNullOrWhiteSpace(title) ? null : title;
            }
        }

        return null;
    }

    private static Guid GetCurrentUserId(ClaimsPrincipal user)
    {
        var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");
        return Guid.TryParse(claimValue, out var userId)
            ? userId
            : Guid.Empty;
    }

    private static IResult ToNotesError(NotesError error)
        => ToError(error.Code, error.Message, ToStatusCode(error.Kind), error.Field, error.Details);

    private static IResult ToError(
        string code,
        string message,
        int statusCode,
        string? field = null,
        IReadOnlyDictionary<string, object?>? details = null)
    {
        var problem = new ProblemDetails
        {
            Title = code,
            Detail = message,
            Status = statusCode
        };
        problem.Extensions["code"] = code;
        problem.Extensions["retryable"] = statusCode is StatusCodes.Status429TooManyRequests or StatusCodes.Status502BadGateway;
        if (!string.IsNullOrWhiteSpace(field))
        {
            problem.Extensions["field"] = field;
        }

        if (details is not null)
        {
            problem.Extensions["details"] = details;
        }

        return TypedResults.Problem(problem);
    }

    private static int ToStatusCode(NotesFailureKind kind)
        => kind switch
        {
            NotesFailureKind.Validation => StatusCodes.Status400BadRequest,
            NotesFailureKind.Forbidden => StatusCodes.Status403Forbidden,
            NotesFailureKind.NotFound => StatusCodes.Status404NotFound,
            NotesFailureKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
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
