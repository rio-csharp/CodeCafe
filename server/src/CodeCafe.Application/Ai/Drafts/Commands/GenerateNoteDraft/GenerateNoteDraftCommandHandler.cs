using CodeCafe.Application.Ai;
using CodeCafe.Application.Notes;
using CodeCafe.Application.Common.Messaging;
using Microsoft.Extensions.Logging;

namespace CodeCafe.Application.Ai.Drafts.Commands.GenerateNoteDraft;

public sealed class GenerateNoteDraftCommandHandler(
    INotebookReadService notebookReadService,
    IAiNoteDraftGenerator draftGenerator,
    ILogger<GenerateNoteDraftCommandHandler> logger)
    : ICommandHandler<GenerateNoteDraftCommand, GenerateNoteDraftResult>
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

    public async Task<GenerateNoteDraftResult> Handle(
        GenerateNoteDraftCommand request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return GenerateNoteDraftResult.Failure(validationError);
        }

        // Resolve the active page from the same notebook load that produces the context; requesting
        // them separately reloads every page's full content a second time.
        var notebookResult = await notebookReadService.GetNotebookContextWithItemAsync(
            request.NotebookSlug.Trim(),
            request.ActivePagePath,
            request.ActorId,
            cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return GenerateNoteDraftResult.Failure(AiFlowError.FromNotesError(notebookResult.Error!));
        }

        var notebook = notebookResult.Value!.Context;
        if (!notebookResult.Value.ActivePageFound)
        {
            return GenerateNoteDraftResult.Failure(new AiFlowError(
                "notebook_item_not_found",
                "Notebook item was not found.",
                AiFailureKind.NotFound,
                "activePagePath"));
        }

        var activePage = notebookResult.Value.ActivePage;
        var normalizedIntent = NormalizeIntent(request.Intent);
        AiNoteDraftResult result;
        try
        {
            result = await draftGenerator.GenerateDraftAsync(
                new AiNoteDraftGenerationContext(
                    request.ActorId,
                    normalizedIntent,
                    request.Prompt.Trim(),
                    AiHelpers.NormalizeLocale(request.Locale),
                    notebook,
                    activePage),
                cancellationToken);
        }
        catch (AiProviderException ex)
        {
            // The caller only sees a generic 502/422, so without this log an upstream outage or a
            // systematically unusable model response is invisible in the server's own telemetry.
            logger.LogWarning(
                ex,
                "AI draft generation failed. Kind={Kind}; Intent={Intent}; NotebookSlug={NotebookSlug}",
                ex.Kind,
                normalizedIntent,
                notebook.Slug);
            return GenerateNoteDraftResult.Failure(new AiFlowError(
                "ai_draft_generation_failed",
                ex.Kind == AiFailureKind.Unprocessable
                    ? "The assistant returned an unparseable or invalid draft. Please rephrase your prompt."
                    : "The assistant could not generate a note draft. Please try again.",
                ex.Kind));
        }

        var markdown = result.Markdown.Trim();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return GenerateNoteDraftResult.Failure(new AiFlowError(
                "empty_ai_draft",
                "The assistant returned an empty draft.",
                AiFailureKind.Unprocessable));
        }

        var title = ExtractTitle(markdown)
            ?? activePage?.Title
            ?? $"{notebook.Title} AI draft";

        return GenerateNoteDraftResult.Success(new AiNoteDraft(
            markdown,
            title,
            normalizedIntent,
            notebook.Slug,
            activePage?.Path));
    }

    private static AiFlowError? ValidateRequest(GenerateNoteDraftCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.NotebookSlug))
        {
            return new AiFlowError("invalid_notebook_slug", "Notebook slug is required.", AiFailureKind.Validation, "notebookSlug");
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return new AiFlowError("invalid_prompt", "Prompt is required.", AiFailureKind.Validation, "prompt");
        }

        return null;
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
}
