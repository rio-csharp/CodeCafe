using System.Text;
using System.Text.Json;
using CodeCafe.Modules.Ai.Configuration;
using CodeCafe.Shared.Application.Identity;
using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Modules.Notes.Application.Notes.Commands.ArchiveNotebookItem;
using CodeCafe.Modules.Notes.Application.Notes.Commands.CreateNotebookItem;
using CodeCafe.Modules.Notes.Application.Notes.Commands.UpdateNotebookItem;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeCafe.Modules.Ai.Edits;

public static class AiNotebookEditEndpoints
{
    private static readonly string[] SupportedOperations =
    [
        "auto",
        "replace_current_page",
        "append_to_current_page",
        "create_page",
        "delete_page",
    ];

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
        INotebookReadService notebookReadService,
        ISender sender,
        ITipTapContentService tipTapContentService,
        IAiNotebookEditGenerator editGenerator,
        IAiNotebookEditProposalStore proposalStore,
        IOptions<AiOptions> aiOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserAccessor.GetCurrentUserId() ?? Guid.Empty;
        if (actorId == Guid.Empty)
        {
            return ToError("authenticated_actor_required", "Authentication is required to generate notebook edits.", StatusCodes.Status401Unauthorized);
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
        if (!notebook.CanEdit)
        {
            return ToError(
                "notebook_forbidden",
                "You do not have permission to edit this notebook.",
                StatusCodes.Status403Forbidden);
        }

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

        var normalizedOperation = NormalizeOperation(request.Operation);
        if (RequiresActivePage(normalizedOperation) && activePage is null)
        {
            return ToError(
                "active_page_required",
                "An active page is required for this AI edit operation.",
                StatusCodes.Status400BadRequest,
                "activePagePath");
        }

        AiNotebookEditResult generatedEdit;
        try
        {
            generatedEdit = await editGenerator.GenerateEditAsync(
                new AiNotebookEditGenerationContext(
                    actorId,
                    normalizedOperation,
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
                "ai_edit_generation_failed",
                "The assistant could not generate a notebook edit. Please try again.",
                StatusCodes.Status502BadGateway);
        }
        catch (Exception ex) when (
            ex is InvalidOperationException
                or JsonException)
        {
            return ToError(
                "ai_edit_generation_failed",
                "The assistant returned an unparseable or invalid edit. Please rephrase your prompt.",
                StatusCodes.Status422UnprocessableEntity);
        }

        if (RequiresActivePage(generatedEdit.Operation) && activePage is null)
        {
            return ToError(
                "active_page_required",
                "The assistant selected a current-page edit, but no active page was provided.",
                StatusCodes.Status400BadRequest,
                "activePagePath");
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
            return ToNotesError(contentResolution.Error!);
        }

        var responseParentPath = ResolveResponseParentPath(notebook, activePage, request.ParentPath);
        var beforeContentJson = createPageProposal ? null : activePage?.ContentJson?.Clone();
        var beforePlainText = createPageProposal ? null : activePage?.PlainTextContent;
        var responseTitle = createPageProposal
            ? generatedEdit.Title!
            : activePage?.Title ?? generatedEdit.Title!;
        var proposal = proposalStore.Save(new AiNotebookEditProposal(
            Guid.NewGuid(),
            actorId,
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
            generatedEdit.Summary ?? BuildFallbackSummary(generatedEdit.Operation, responseTitle, activePage?.Path)));

        if (request.Apply)
        {
            var appliedResult = await ApplyProposalAsync(
                proposal,
                notebookReadService,
                sender,
                proposalStore,
                aiOptionsAccessor.Value.EditEndpointPath,
                actorId,
                cancellationToken);
            return appliedResult;
        }

        return TypedResults.Ok(ToResponse(proposal, aiOptionsAccessor.Value.EditEndpointPath, applied: false, savedAtUtc: null));
    }

    private static async Task<IResult> GetNotebookEditProposalAsync(
        Guid proposalId,
        HttpContext httpContext,
        ICurrentUserAccessor currentUserAccessor,
        IAiNotebookEditProposalStore proposalStore,
        INotebookReadService notebookReadService,
        IOptions<AiOptions> aiOptionsAccessor)
    {
        var actorId = currentUserAccessor.GetCurrentUserId() ?? Guid.Empty;
        if (actorId == Guid.Empty)
        {
            return ToError("authenticated_actor_required", "Authentication is required to preview notebook edits.", StatusCodes.Status401Unauthorized);
        }

        if (!proposalStore.TryGet(proposalId, actorId, out var proposal))
        {
            return ToError("ai_edit_proposal_not_found", "The notebook edit proposal was not found or has expired.", StatusCodes.Status404NotFound, "proposalId");
        }

        var notebookResult = await notebookReadService.GetNotebookContextAsync(
            proposal.NotebookSlug,
            actorId,
            httpContext.RequestAborted);
        if (!notebookResult.Succeeded)
        {
            return ToNotesError(notebookResult.Error!);
        }

        if (proposal.PagePath is not null
            && proposal.EffectiveOperation != "create_page"
            && ResolveActivePage(notebookResult.Value!, proposal.PagePath) is null)
        {
            return ToError("notebook_item_not_found", "Notebook item was not found.", StatusCodes.Status404NotFound, "pagePath");
        }

        return TypedResults.Ok(ToResponse(proposal, aiOptionsAccessor.Value.EditEndpointPath, applied: false, savedAtUtc: null));
    }

    private static async Task<IResult> ApplyNotebookEditProposalAsync(
        Guid proposalId,
        ICurrentUserAccessor currentUserAccessor,
        INotebookReadService notebookReadService,
        ISender sender,
        IAiNotebookEditProposalStore proposalStore,
        IOptions<AiOptions> aiOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserAccessor.GetCurrentUserId() ?? Guid.Empty;
        if (actorId == Guid.Empty)
        {
            return ToError("authenticated_actor_required", "Authentication is required to apply notebook edits.", StatusCodes.Status401Unauthorized);
        }

        if (!proposalStore.TryGet(proposalId, actorId, out var proposal))
        {
            return ToError("ai_edit_proposal_not_found", "The notebook edit proposal was not found or has expired.", StatusCodes.Status404NotFound, "proposalId");
        }

        return await ApplyProposalAsync(
            proposal,
            notebookReadService,
            sender,
            proposalStore,
            aiOptionsAccessor.Value.EditEndpointPath,
            actorId,
            cancellationToken);
    }

    private static IResult DiscardNotebookEditProposalAsync(
        Guid proposalId,
        ICurrentUserAccessor currentUserAccessor,
        IAiNotebookEditProposalStore proposalStore)
    {
        var actorId = currentUserAccessor.GetCurrentUserId() ?? Guid.Empty;
        if (actorId == Guid.Empty)
        {
            return ToError("authenticated_actor_required", "Authentication is required to discard notebook edits.", StatusCodes.Status401Unauthorized);
        }

        if (!proposalStore.TryGet(proposalId, actorId, out _))
        {
            return ToError("ai_edit_proposal_not_found", "The notebook edit proposal was not found or has expired.", StatusCodes.Status404NotFound, "proposalId");
        }

        proposalStore.Remove(proposalId);
        return TypedResults.Ok(new { proposalId, discarded = true });
    }

    private static async Task<IResult> ApplyProposalAsync(
        AiNotebookEditProposal proposal,
        INotebookReadService notebookReadService,
        ISender sender,
        IAiNotebookEditProposalStore proposalStore,
        string editEndpointPath,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var notebookResult = await notebookReadService.GetNotebookContextAsync(
            proposal.NotebookSlug,
            actorId,
            cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return ToNotesError(notebookResult.Error!);
        }

        var notebook = notebookResult.Value!;
        DateTimeOffset? savedAtUtc;
        string? pagePath = proposal.PagePath;
        Guid? pageId = proposal.PageId;
        string? parentPath = proposal.ParentPath;

        if (proposal.EffectiveOperation == "create_page")
        {
            var parent = ResolveParentForCreate(notebook, proposal.ParentPath, null);
            if (!parent.Succeeded)
            {
                return ToNotesError(parent.Error!);
            }

            var createResult = await sender.Send(
                new CreateNotebookItemCommand(
                    notebook.Id,
                    actorId,
                    parent.Value?.Id,
                    "page",
                    proposal.Title,
                    ResolveCreateSortOrder(notebook, parent.Value?.Id),
                    proposal.AfterContentJson),
                cancellationToken);
            if (!createResult.Succeeded)
            {
                return ToNotesError(createResult.Error!);
            }

            var createdPage = createResult.Value!;
            pageId = createdPage.Id;
            pagePath = createdPage.Path;
            parentPath = ResolveParentPathFromItem(notebook, createdPage.ParentId);
            savedAtUtc = createdPage.UpdatedAtUtc ?? createdPage.CreatedAtUtc;
        }
        else if (proposal.EffectiveOperation == "delete_page")
        {
            if (proposal.PageId is null)
            {
                return ToError("active_page_required", "An active page is required for this AI edit operation.", StatusCodes.Status400BadRequest, "activePagePath");
            }

            var archiveResult = await sender.Send(
                new ArchiveNotebookItemCommand(
                    notebook.Id,
                    proposal.PageId.Value,
                    actorId),
                cancellationToken);
            if (!archiveResult.Succeeded)
            {
                return ToNotesError(archiveResult.Error!);
            }

            var archivedPage = archiveResult.Value!;
            pageId = archivedPage.Id;
            pagePath = archivedPage.Path;
            savedAtUtc = archivedPage.UpdatedAtUtc ?? archivedPage.CreatedAtUtc;
        }
        else
        {
            if (proposal.PageId is null)
            {
                return ToError("active_page_required", "An active page is required for this AI edit operation.", StatusCodes.Status400BadRequest, "activePagePath");
            }

            var updateResult = await sender.Send(
                new UpdateNotebookItemCommand(
                    notebook.Id,
                    proposal.PageId.Value,
                    actorId,
                    proposal.Title,
                    default,
                    null,
                    proposal.AfterContentJson,
                    proposal.SourcePageUpdatedAtUtc),
                cancellationToken);
            if (!updateResult.Succeeded)
            {
                return ToNotesError(updateResult.Error!);
            }

            var updatedPage = updateResult.Value!;
            pageId = updatedPage.Id;
            pagePath = updatedPage.Path;
            savedAtUtc = updatedPage.UpdatedAtUtc ?? updatedPage.CreatedAtUtc;
        }

        proposalStore.Remove(proposal.ProposalId);
        var appliedProposal = proposal with
        {
            PageId = pageId,
            PagePath = pagePath,
            ParentPath = parentPath
        };

        return TypedResults.Ok(ToResponse(appliedProposal, editEndpointPath, applied: true, savedAtUtc));
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

    private static IResult? ValidateRequest(AiNotebookEditRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NotebookSlug))
        {
            return ToError("invalid_notebook_slug", "Notebook slug is required.", StatusCodes.Status400BadRequest, "notebookSlug");
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return ToError("invalid_prompt", "Prompt is required.", StatusCodes.Status400BadRequest, "prompt");
        }

        var operation = NormalizeOperation(request.Operation);
        return SupportedOperations.Contains(operation, StringComparer.Ordinal)
            ? null
            : ToError("invalid_operation", "Operation must be auto, replace_current_page, append_to_current_page, create_page, or delete_page.", StatusCodes.Status400BadRequest, "operation");
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

    private static NotesResult<NotebookContextItemModel?> ResolveParentForCreate(
        NotebookContextModel notebook,
        string? parentPath,
        NotebookItemModel? activePage)
    {
        if (!string.IsNullOrWhiteSpace(parentPath))
        {
            var normalizedPath = NotebookInput.NormalizePath(parentPath);
            var parent = notebook.Items.SingleOrDefault(item =>
                string.Equals(item.Path, normalizedPath, StringComparison.Ordinal)
                && string.Equals(item.Type, "folder", StringComparison.OrdinalIgnoreCase));

            return parent is null
                ? NotesResult<NotebookContextItemModel?>.Failure(NotesFailureKind.Validation, "invalid_parent", "Parent folder was not found.")
                : NotesResult<NotebookContextItemModel?>.Success(parent);
        }

        if (activePage?.ParentId is Guid activeParentId)
        {
            var parent = notebook.Items.SingleOrDefault(item => item.Id == activeParentId);
            return NotesResult<NotebookContextItemModel?>.Success(parent);
        }

        return NotesResult<NotebookContextItemModel?>.Success(null);
    }

    private static int ResolveCreateSortOrder(NotebookContextModel notebook, Guid? parentId)
    {
        var siblings = notebook.Items.Where(item => item.ParentId == parentId).ToList();
        return siblings.Count == 0 ? 0 : siblings.Max(item => item.SortOrder) + 1;
    }

    private static string? ResolveResponseParentPath(NotebookContextModel notebook, NotebookItemModel? activePage, string? requestedParentPath)
    {
        if (!string.IsNullOrWhiteSpace(requestedParentPath))
        {
            return NotebookInput.NormalizePath(requestedParentPath);
        }

        return activePage is null
            ? null
            : ResolveParentPathFromItem(notebook, activePage.ParentId);
    }

    private static string? ResolveParentPathFromItem(NotebookContextModel notebook, Guid? parentId)
    {
        return parentId is null
            ? null
            : notebook.Items.SingleOrDefault(item => item.Id == parentId)?.Path;
    }

    private static bool RequiresActivePage(string operation)
        => operation is "replace_current_page" or "append_to_current_page" or "delete_page";

    private static string NormalizeOperation(string? operation)
        => string.IsNullOrWhiteSpace(operation)
            ? "auto"
            : operation.Trim().ToLowerInvariant();

    private static string NormalizeLocale(string? locale)
        => string.IsNullOrWhiteSpace(locale) ? "en" : locale.Trim();

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

    private sealed record ResolvedGeneratedContent(
        JsonElement AfterContentJson,
        string? AfterPlainTextContent);
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
