using CodeCafe.Modules.Ai.Common;
using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Shared.Application.Common.Abstractions.Messaging;
using Microsoft.AspNetCore.Http;

namespace CodeCafe.Modules.Ai.Drafts.Commands.GenerateNoteDraft;

public sealed class GenerateNoteDraftCommandHandler(
    INotebookReadService notebookReadService,
    IAiNoteDraftGenerator draftGenerator)
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

        var notebookResult = await notebookReadService.GetNotebookContextAsync(
            request.NotebookSlug.Trim(),
            request.ActorId,
            cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return GenerateNoteDraftResult.Failure(AiFlowError.FromNotesError(notebookResult.Error!));
        }

        var notebook = notebookResult.Value!;
        var activePageItem = AiHelpers.ResolveActivePage(notebook, request.ActivePagePath);
        if (request.ActivePagePath is not null && activePageItem is null)
        {
            return GenerateNoteDraftResult.Failure(new AiFlowError(
                "notebook_item_not_found",
                "Notebook item was not found.",
                StatusCodes.Status404NotFound,
                "activePagePath"));
        }

        NotebookItemModel? activePage = null;
        if (activePageItem is not null)
        {
            var activePageResult = await notebookReadService.GetNotebookItemByPathAsync(
                notebook.Slug,
                activePageItem.Path,
                request.ActorId,
                cancellationToken);
            if (!activePageResult.Succeeded)
            {
                return GenerateNoteDraftResult.Failure(AiFlowError.FromNotesError(activePageResult.Error!));
            }

            activePage = activePageResult.Value;
        }

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
        catch (Exception ex) when (
            ex is System.ClientModel.ClientResultException
                or HttpRequestException)
        {
            return GenerateNoteDraftResult.Failure(new AiFlowError(
                "ai_draft_generation_failed",
                "The assistant could not generate a note draft. Please try again.",
                StatusCodes.Status502BadGateway));
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            return GenerateNoteDraftResult.Failure(new AiFlowError(
                "ai_draft_generation_failed",
                "The assistant returned an unparseable or invalid draft. Please rephrase your prompt.",
                StatusCodes.Status422UnprocessableEntity));
        }

        var markdown = result.Markdown.Trim();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return GenerateNoteDraftResult.Failure(new AiFlowError(
                "empty_ai_draft",
                "The assistant returned an empty draft.",
                StatusCodes.Status422UnprocessableEntity));
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
            return new AiFlowError("invalid_notebook_slug", "Notebook slug is required.", StatusCodes.Status400BadRequest, "notebookSlug");
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return new AiFlowError("invalid_prompt", "Prompt is required.", StatusCodes.Status400BadRequest, "prompt");
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
