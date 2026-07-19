using CodeCafe.Modules.Ai.Configuration;
using CodeCafe.Shared.Application.Identity;
using CodeCafe.Modules.Notes.Application.Notes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeCafe.Modules.Ai.Drafts;

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
        ICurrentUserAccessor currentUserAccessor,
        INotebookReadService notebookReadService,
        IAiNoteDraftGenerator draftGenerator,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserAccessor.GetCurrentUserId() ?? Guid.Empty;
        if (actorId == Guid.Empty)
        {
            return ToError("authenticated_actor_required", "Authentication is required to generate note drafts.", StatusCodes.Status401Unauthorized);
        }

        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return validationError;
        }

        var notebookResult = await notebookReadService.GetNotebookContextAsync(
            request.NotebookSlug.Trim(),
            actorId,
            cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return ToNotesError(notebookResult.Error!);
        }

        var notebook = notebookResult.Value!;
        var activePageItem = ResolveActivePage(notebook, request.ActivePagePath);
        if (request.ActivePagePath is not null && activePageItem is null)
        {
            return ToError("notebook_item_not_found", "Notebook item was not found.", StatusCodes.Status404NotFound, "activePagePath");
        }

        NotebookItemModel? activePage = null;
        if (activePageItem is not null)
        {
            var activePageResult = await notebookReadService.GetNotebookItemByPathAsync(
                notebook.Slug,
                activePageItem.Path,
                actorId,
                cancellationToken);
            if (!activePageResult.Succeeded)
            {
                return ToNotesError(activePageResult.Error!);
            }

            activePage = activePageResult.Value;
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
                    notebook,
                    activePage),
                cancellationToken);
        }
        catch (Exception ex) when (
            ex is System.ClientModel.ClientResultException
                or HttpRequestException)
        {
            return ToError(
                "ai_draft_generation_failed",
                "The assistant could not generate a note draft. Please try again.",
                StatusCodes.Status502BadGateway);
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            return ToError(
                "ai_draft_generation_failed",
                "The assistant returned an unparseable or invalid draft. Please rephrase your prompt.",
                StatusCodes.Status422UnprocessableEntity);
        }

        var markdown = result.Markdown.Trim();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return ToError("empty_ai_draft", "The assistant returned an empty draft.", StatusCodes.Status422UnprocessableEntity);
        }

        var title = ExtractTitle(markdown)
            ?? activePage?.Title
            ?? $"{notebook.Title} AI draft";

        return TypedResults.Ok(new AiNoteDraftResponse(
            markdown,
            title,
            normalizedIntent,
            notebook.Slug,
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

    private static NotebookContextItemModel? ResolveActivePage(NotebookContextModel notebook, string? activePagePath)
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
