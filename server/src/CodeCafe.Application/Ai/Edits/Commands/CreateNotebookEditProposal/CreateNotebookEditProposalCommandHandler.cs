using CodeCafe.Application.Ai;
using CodeCafe.Application.Ai.Edits.Commands.ApplyNotebookEditProposal;
using CodeCafe.Application.Notes;
using CodeCafe.Application.Common.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CodeCafe.Application.Ai.Edits.Commands.CreateNotebookEditProposal;

public sealed class CreateNotebookEditProposalCommandHandler(
    INotebookReadService notebookReadService,
    ISender sender,
    ITipTapContentService tipTapContentService,
    IAiNotebookEditGenerator editGenerator,
    IAiNotebookEditProposalStore proposalStore,
    ILogger<CreateNotebookEditProposalCommandHandler> logger)
    : ICommandHandler<CreateNotebookEditProposalCommand, AiEditProposalFlowResult>
{
    private static readonly string[] SupportedOperations =
    [
        "auto",
        "replace_current_page",
        "append_to_current_page",
        "create_page",
        "delete_page",
    ];

    public async Task<AiEditProposalFlowResult> Handle(
        CreateNotebookEditProposalCommand request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return AiEditProposalFlowResult.Failure(validationError);
        }

        // One notebook load serves both the context and the active page; the two-call form reloaded
        // every page's full content a second time.
        var notebookResult = await notebookReadService.GetNotebookContextWithItemAsync(
            request.NotebookSlug.Trim(),
            request.ActivePagePath,
            request.ActorId,
            cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return AiEditProposalFlowResult.Failure(notebookResult.Error!);
        }

        var notebook = notebookResult.Value!.Context;
        if (!notebook.CanEdit)
        {
            return AiEditProposalFlowResult.Failure(new AiFlowError(
                "notebook_forbidden",
                "You do not have permission to edit this notebook.",
                AiFailureKind.Forbidden));
        }

        if (!notebookResult.Value.ActivePageFound)
        {
            return AiEditProposalFlowResult.Failure(new AiFlowError(
                "notebook_item_not_found",
                "Notebook item was not found.",
                AiFailureKind.NotFound,
                "activePagePath"));
        }

        var activePage = notebookResult.Value.ActivePage;
        var normalizedOperation = NormalizeOperation(request.Operation);
        if (RequiresActivePage(normalizedOperation) && activePage is null)
        {
            return AiEditProposalFlowResult.Failure(new AiFlowError(
                "active_page_required",
                "An active page is required for this AI edit operation.",
                AiFailureKind.Validation,
                "activePagePath"));
        }

        AiNotebookEditResult generatedEdit;
        try
        {
            generatedEdit = await editGenerator.GenerateEditAsync(
                new AiNotebookEditGenerationContext(
                    request.ActorId,
                    normalizedOperation,
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
                "AI edit generation failed. Kind={Kind}; Operation={Operation}; NotebookSlug={NotebookSlug}",
                ex.Kind,
                normalizedOperation,
                notebook.Slug);
            return AiEditProposalFlowResult.Failure(new AiFlowError(
                "ai_edit_generation_failed",
                ex.Kind == AiFailureKind.Unprocessable
                    ? "The assistant returned an unparseable or invalid edit. Please rephrase your prompt."
                    : "The assistant could not generate a notebook edit. Please try again.",
                ex.Kind));
        }

        if (RequiresActivePage(generatedEdit.Operation) && activePage is null)
        {
            return AiEditProposalFlowResult.Failure(new AiFlowError(
                "active_page_required",
                "The assistant selected a current-page edit, but no active page was provided.",
                AiFailureKind.Validation,
                "activePagePath"));
        }

        var createPageProposal = generatedEdit.Operation == "create_page";
        var deletePageProposal = generatedEdit.Operation == "delete_page";
        var contentResolution = deletePageProposal
            ? NotesResult<ResolvedGeneratedContent>.Success(new ResolvedGeneratedContent(TipTapDocumentOperations.CreateEmptyDocument(), null))
            : ResolveGeneratedContent(
                generatedEdit,
                createPageProposal ? null : activePage?.ContentJson,
                tipTapContentService);
        if (!contentResolution.Succeeded)
        {
            return AiEditProposalFlowResult.Failure(contentResolution.Error!);
        }

        var responseParentPath = ResolveResponseParentPath(notebook, activePage, request.ParentPath);
        var beforeContentJson = createPageProposal ? null : activePage?.ContentJson?.Clone();
        var beforePlainText = createPageProposal ? null : activePage?.PlainTextContent;
        var responseTitle = createPageProposal
            ? generatedEdit.Title!
            : activePage?.Title ?? generatedEdit.Title!;
        var proposal = await proposalStore.SaveAsync(new AiNotebookEditProposal(
            Guid.NewGuid(),
            request.ActorId,
            normalizedOperation,
            generatedEdit.Operation,
            generatedEdit.Mode,
            notebook.Id,
            notebook.Slug,
            notebook.Title,
            createPageProposal ? null : activePage?.Id,
            responseTitle,
            createPageProposal ? null : activePage?.Path,
            responseParentPath,
            beforeContentJson,
            beforePlainText,
            contentResolution.Value!.AfterContentJson,
            contentResolution.Value.AfterPlainTextContent,
            generatedEdit.OperationsJson,
            request.ExpectedUpdatedAtUtc ?? activePage?.UpdatedAtUtc ?? activePage?.CreatedAtUtc ?? DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(30),
            generatedEdit.Summary ?? BuildFallbackSummary(generatedEdit.Operation, responseTitle, activePage?.Path)),
            cancellationToken);

        if (request.Apply)
        {
            return await sender.Send(
                new ApplyNotebookEditProposalCommand(proposal.ProposalId, request.ActorId),
                cancellationToken);
        }

        return AiEditProposalFlowResult.Success(proposal, applied: false, savedAtUtc: null);
    }

    private static AiFlowError? ValidateRequest(CreateNotebookEditProposalCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.NotebookSlug))
        {
            return new AiFlowError("invalid_notebook_slug", "Notebook slug is required.", AiFailureKind.Validation, "notebookSlug");
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return new AiFlowError("invalid_prompt", "Prompt is required.", AiFailureKind.Validation, "prompt");
        }

        var operation = NormalizeOperation(request.Operation);
        return SupportedOperations.Contains(operation, StringComparer.Ordinal)
            ? null
            : new AiFlowError("invalid_operation", "Operation must be auto, replace_current_page, append_to_current_page, create_page, or delete_page.", AiFailureKind.Validation, "operation");
    }

    private static NotesResult<ResolvedGeneratedContent> ResolveGeneratedContent(
        AiNotebookEditResult generatedEdit,
        JsonElement? existingContentJson,
        ITipTapContentService tipTapContentService)
    {
        JsonElement nextContentJson;
        try
        {
            nextContentJson = generatedEdit.Mode == "operations"
                ? TipTapDocumentOperations.ApplyOperations(existingContentJson, generatedEdit.OperationsJson ?? default)
                : generatedEdit.ContentJson?.Clone() ?? TipTapDocumentOperations.CreateEmptyDocument();
        }
        catch (ArgumentException exception)
        {
            return NotesResult<ResolvedGeneratedContent>.Failure(
                NotesFailureKind.Validation,
                "invalid_ai_edit_operations",
                exception.Message,
                "operations");
        }

        var normalizedContent = tipTapContentService.NormalizePageContent(nextContentJson);
        if (!normalizedContent.Succeeded)
        {
            return NotesResult<ResolvedGeneratedContent>.Failure(
                normalizedContent.Error!.Kind,
                normalizedContent.Error.Code,
                normalizedContent.Error.Message,
                normalizedContent.Error.Field,
                normalizedContent.Error.Details);
        }

        return NotesResult<ResolvedGeneratedContent>.Success(new ResolvedGeneratedContent(
            ParseNormalizedDocument(normalizedContent.Value!.ContentJson!),
            normalizedContent.Value.PlainTextContent));
    }

    private static string? ResolveResponseParentPath(NotebookContextModel notebook, NotebookItemModel? activePage, string? requestedParentPath)
    {
        if (!string.IsNullOrWhiteSpace(requestedParentPath))
        {
            return NotebookInput.NormalizePath(requestedParentPath);
        }

        return activePage is null
            ? null
            : NotebookContextTree.ResolveParentPathFromItem(notebook, activePage.ParentId);
    }

    private static bool RequiresActivePage(string operation)
        => operation is "replace_current_page" or "append_to_current_page" or "delete_page";

    private static string NormalizeOperation(string? operation)
        => string.IsNullOrWhiteSpace(operation)
            ? "auto"
            : operation.Trim().ToLowerInvariant();

    private static JsonElement ParseNormalizedDocument(string normalizedJson)
    {
        using var document = JsonDocument.Parse(normalizedJson);
        return document.RootElement.Clone();
    }

    private static string BuildFallbackSummary(string operation, string title, string? pagePath)
        => operation switch
        {
            "create_page" => $"Create page '{title}'.",
            "append_to_current_page" => $"Append content to '{pagePath ?? title}'.",
            "delete_page" => $"Delete page '{pagePath ?? title}'.",
            _ => $"Update '{pagePath ?? title}'."
        };

    private sealed record ResolvedGeneratedContent(
        JsonElement AfterContentJson,
        string? AfterPlainTextContent);
}
