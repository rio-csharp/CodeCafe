using CodeCafe.Application.Common.Configuration;
using CodeCafe.Application.Common.Identity;
using CodeCafe.Application.Common.Uploads;
using CodeCafe.Application.Notes;
using CodeCafe.Application.Notes.Commands.CreateNotebookItem;
using CodeCafe.Application.Notes.Commands.UpdateNotebookItem;
using CodeCafe.Host.Common;
using MediatR;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodeCafe.Host.Rest.Notes;

public static class NotesMarkdownImportEndpoints
{
    public static IEndpointRouteBuilder MapNotesMarkdownImportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/notes")
            .WithTags("Notes")
            .RequireAuthorization();

        group.MapPost("/uploads/markdown", UploadMarkdownAsync);
        group.MapDelete("/uploads/{uploadId}", DeleteUploadAsync);
        group.MapPost("/notebooks/{notebookSlug}/pages/import-markdown", ImportMarkdownPageAsync);
        group.MapPut("/notebooks/{notebookSlug}/pages/{**pathAndAction}", ReplacePageContentFromMarkdownAsync);
        group.MapPost("/notebooks/{notebookSlug}/pages/{**pathAndAction}", AppendMarkdownToPageAsync);

        return endpoints;
    }

    private static async Task<IResult> UploadMarkdownAsync(
        HttpRequest request,
        ICurrentUserAccessor currentUserAccessor,
        IUploadStore uploadStore,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserAccessor.GetCurrentUserId() ?? Guid.Empty;
        if (actorId == Guid.Empty)
        {
            return ToError("authenticated_actor_required", "The Notes API requires an authenticated CodeCafe user.", StatusCodes.Status401Unauthorized);
        }

        IFormFile? file = null;
        string? requestedFileName = null;
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync(cancellationToken);
            file = form.Files.GetFile("file");
            requestedFileName = form["fileName"].ToString();
        }

        var options = mcpOptionsAccessor.Value;
        var validationError = MarkdownUploadValidation.Validate(
            request.HasFormContentType,
            file is null ? null : new MarkdownUploadFile(file.ContentType, file.FileName, file.Length),
            requestedFileName,
            options.MaxUploadBytes,
            "Upload exceeds maxUploadBytes.",
            "Only Markdown uploads are supported.",
            out var validatedUpload);
        if (validationError is not null)
        {
            return ToError(
                validationError.Code,
                validationError.Message,
                StatusCodes.Status400BadRequest,
                field: validationError.Field,
                details: validationError.Details);
        }

        string contentText;
        await using (var stream = file!.OpenReadStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
        {
            contentText = await reader.ReadToEndAsync(cancellationToken);
        }

        var uploadResult = await uploadStore.CreateTextAsync(
            actorId,
            validatedUpload!.FileName,
            validatedUpload.MediaType,
            contentText,
            options.MaxUploadBytes,
            cancellationToken);
        if (!uploadResult.Succeeded)
        {
            return ToUploadError(uploadResult.Error!, StatusCodes.Status400BadRequest);
        }

        var upload = uploadResult.Value!;
        return TypedResults.Ok(new NotesMarkdownUploadResponse(
            upload.UploadId,
            upload.FileName,
            upload.MediaType,
            upload.BytesReceived,
            upload.UpdatedAtUtc.AddSeconds(options.UploadIdleTimeoutSeconds)));
    }

    private static async Task<IResult> DeleteUploadAsync(
        string uploadId,
        ICurrentUserAccessor currentUserAccessor,
        IUploadStore uploadStore,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserAccessor.GetCurrentUserId() ?? Guid.Empty;
        if (actorId == Guid.Empty)
        {
            return ToError("authenticated_actor_required", "The Notes API requires an authenticated CodeCafe user.", StatusCodes.Status401Unauthorized);
        }

        var removed = await uploadStore.DeleteAsync(actorId, uploadId, cancellationToken);
        return TypedResults.Ok(new NotesDiscardUploadResponse(uploadId, removed ? "discarded" : "already_absent"));
    }

    private static async Task<IResult> ImportMarkdownPageAsync(
        string notebookSlug,
        CreateMarkdownPageImportRequest request,
        ICurrentUserAccessor currentUserAccessor,
        INotebookReadService notebookReadService,
        ISender sender,
        IContentImporter contentImportService,
        CancellationToken cancellationToken)
    {
        var actorId = currentUserAccessor.GetCurrentUserId() ?? Guid.Empty;
        if (actorId == Guid.Empty)
        {
            return ToError("authenticated_actor_required", "The Notes API requires an authenticated CodeCafe user.", StatusCodes.Status401Unauthorized);
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return ToError("invalid_title", "Page title is required and cannot be empty or whitespace.", StatusCodes.Status400BadRequest, field: "title");
        }

        if (string.IsNullOrWhiteSpace(request.UploadId))
        {
            return ToError("upload_not_found", "UploadId is required.", StatusCodes.Status400BadRequest, field: "uploadId");
        }

        var notebookResult = await notebookReadService.GetNotebookSummaryBySlugAsync(notebookSlug, actorId, cancellationToken);
        if (!notebookResult.Succeeded)
        {
            return ToNotesError(notebookResult.Error!);
        }

        var parentResult = await ResolveParentAsync(notebookSlug, request.ParentPath, actorId, notebookReadService, cancellationToken);
        if (!parentResult.Succeeded)
        {
            return ToNotesError(parentResult.Error!);
        }

        var contentResult = await contentImportService.ResolveRequiredPageContentAsync(
            actorId,
            inlineContentJson: null,
            request.UploadId,
            "markdown",
            "invalid_content_upload",
            "UploadId must reference uploaded Markdown content.",
            cancellationToken);
        if (!contentResult.Succeeded)
        {
            return ToNotesError(contentResult.Error!);
        }

        var sizeResult = contentImportService.EnforcePageContentSize(contentResult.Value, "content_too_large");
        if (!sizeResult.Succeeded)
        {
            return ToNotesError(sizeResult.Error!);
        }

        var createResult = await sender.Send(
            new CreateNotebookItemCommand(
                notebookResult.Value!.Id,
                actorId,
                parentResult.Value?.Id,
                "page",
                request.Title,
                0,
                contentResult.Value),
            cancellationToken);
        if (!createResult.Succeeded)
        {
            return ToNotesError(createResult.Error!);
        }

        await contentImportService.DeleteUploadAsync(actorId, request.UploadId, cancellationToken);

        var response = ToPageResponse(createResult.Value!, notebookSlug, request.IncludeContent);
        return TypedResults.Created($"/api/notes/notebooks/{notebookSlug}/pages/{response.Path}", response);
    }

    private static async Task<IResult> ReplacePageContentFromMarkdownAsync(
        string notebookSlug,
        string pathAndAction,
        UpdateMarkdownPageImportRequest request,
        ICurrentUserAccessor currentUserAccessor,
        INotebookReadService notebookReadService,
        ISender sender,
        IContentImporter contentImportService,
        CancellationToken cancellationToken)
    {
        if (!TryExtractPagePath(pathAndAction, "import-markdown", out var path))
        {
            return Results.NotFound();
        }

        var actorId = currentUserAccessor.GetCurrentUserId() ?? Guid.Empty;
        if (actorId == Guid.Empty)
        {
            return ToError("authenticated_actor_required", "The Notes API requires an authenticated CodeCafe user.", StatusCodes.Status401Unauthorized);
        }

        if (string.IsNullOrWhiteSpace(request.UploadId))
        {
            return ToError("upload_not_found", "UploadId is required.", StatusCodes.Status400BadRequest, field: "uploadId");
        }

        var pageResult = await RequirePageAsync(notebookSlug, path, actorId, notebookReadService, cancellationToken);
        if (!pageResult.Succeeded)
        {
            return ToNotesError(pageResult.Error!);
        }

        var contentResult = await contentImportService.ResolveRequiredPageContentAsync(
            actorId,
            inlineContentJson: null,
            request.UploadId,
            "markdown",
            "invalid_content_upload",
            "UploadId must reference uploaded Markdown content.",
            cancellationToken);
        if (!contentResult.Succeeded)
        {
            return ToNotesError(contentResult.Error!);
        }

        var sizeResult = contentImportService.EnforcePageContentSize(contentResult.Value, "content_too_large");
        if (!sizeResult.Succeeded)
        {
            return ToNotesError(sizeResult.Error!);
        }

        var updateResult = await sender.Send(
            new UpdateNotebookItemCommand(
                pageResult.Value!.NotebookId,
                pageResult.Value.Id,
                actorId,
                pageResult.Value.Title,
                default,
                null,
                contentResult.Value,
                // Replacing content is a read-modify-write over the page loaded above; passing the
                // timestamp that was read turns a concurrent edit into a 409 instead of silently
                // discarding it.
                pageResult.Value.UpdatedAtUtc),
            cancellationToken);
        if (!updateResult.Succeeded)
        {
            return ToNotesError(updateResult.Error!);
        }

        await contentImportService.DeleteUploadAsync(actorId, request.UploadId, cancellationToken);

        return TypedResults.Ok(ToPageResponse(updateResult.Value!, notebookSlug, request.IncludeContent));
    }

    private static async Task<IResult> AppendMarkdownToPageAsync(
        string notebookSlug,
        string pathAndAction,
        UpdateMarkdownPageImportRequest request,
        ICurrentUserAccessor currentUserAccessor,
        INotebookReadService notebookReadService,
        ISender sender,
        IContentImporter contentImportService,
        CancellationToken cancellationToken)
    {
        if (!TryExtractPagePath(pathAndAction, "append-markdown", out var path))
        {
            return Results.NotFound();
        }

        var actorId = currentUserAccessor.GetCurrentUserId() ?? Guid.Empty;
        if (actorId == Guid.Empty)
        {
            return ToError("authenticated_actor_required", "The Notes API requires an authenticated CodeCafe user.", StatusCodes.Status401Unauthorized);
        }

        if (string.IsNullOrWhiteSpace(request.UploadId))
        {
            return ToError("upload_not_found", "UploadId is required.", StatusCodes.Status400BadRequest, field: "uploadId");
        }

        var pageResult = await RequirePageAsync(notebookSlug, path, actorId, notebookReadService, cancellationToken);
        if (!pageResult.Succeeded)
        {
            return ToNotesError(pageResult.Error!);
        }

        var blocksResult = await contentImportService.ResolveRequiredBlocksAsync(
            actorId,
            inlineBlocks: null,
            request.UploadId,
            "markdown",
            "invalid_blocks",
            "UploadId must reference uploaded Markdown content.",
            cancellationToken);
        if (!blocksResult.Succeeded)
        {
            return ToNotesError(blocksResult.Error!);
        }

        JsonElement nextContentJson;
        try
        {
            nextContentJson = AppendBlocks(pageResult.Value!.ContentJson, blocksResult.Value);
        }
        catch (ArgumentException exception)
        {
            return ToError("invalid_blocks", exception.Message, StatusCodes.Status400BadRequest, field: "uploadId");
        }

        var sizeResult = contentImportService.EnforcePageContentSize(nextContentJson, "content_too_large");
        if (!sizeResult.Succeeded)
        {
            return ToNotesError(sizeResult.Error!);
        }

        var updateResult = await sender.Send(
            new UpdateNotebookItemCommand(
                pageResult.Value.NotebookId,
                pageResult.Value.Id,
                actorId,
                pageResult.Value.Title,
                default,
                null,
                nextContentJson,
                // The appended content was computed from the ContentJson read above, so the write
                // must be conditional on that timestamp or a concurrent edit is silently dropped.
                pageResult.Value.UpdatedAtUtc),
            cancellationToken);
        if (!updateResult.Succeeded)
        {
            return ToNotesError(updateResult.Error!);
        }

        await contentImportService.DeleteUploadAsync(actorId, request.UploadId, cancellationToken);

        return TypedResults.Ok(ToPageResponse(updateResult.Value!, notebookSlug, request.IncludeContent));
    }

    private static async Task<NotesResult<NotebookItemModel?>> ResolveParentAsync(
        string notebookSlug,
        string? parentPath,
        Guid actorId,
        INotebookReadService notebookReadService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(parentPath))
        {
            return NotesResult<NotebookItemModel?>.Success(null);
        }

        var parentResult = await notebookReadService.GetNotebookItemByPathAsync(
            notebookSlug,
            parentPath,
            actorId,
            cancellationToken);
        if (!parentResult.Succeeded)
        {
            return NotesResult<NotebookItemModel?>.Failure(
                parentResult.Error!.Kind,
                parentResult.Error.Code,
                parentResult.Error.Message,
                parentResult.Error.Field,
                parentResult.Error.Details);
        }

        return string.Equals(parentResult.Value!.Type, "folder", StringComparison.OrdinalIgnoreCase)
            ? NotesResult<NotebookItemModel?>.Success(parentResult.Value)
            : NotesResult<NotebookItemModel?>.Failure(
                NotesFailureKind.Validation,
                "invalid_parent",
                "Parent item must be a folder.",
                "parentPath");
    }

    private static async Task<NotesResult<NotebookItemModel>> RequirePageAsync(
        string notebookSlug,
        string path,
        Guid actorId,
        INotebookReadService notebookReadService,
        CancellationToken cancellationToken)
    {
        var pageResult = await notebookReadService.GetNotebookItemByPathAsync(
            notebookSlug,
            path,
            actorId,
            cancellationToken);
        if (!pageResult.Succeeded)
        {
            return pageResult;
        }

        return string.Equals(pageResult.Value!.Type, "page", StringComparison.OrdinalIgnoreCase)
            ? pageResult
            : NotesResult<NotebookItemModel>.Failure(
                NotesFailureKind.Validation,
                "page_required",
                "The requested notebook item is not a page.",
                "path");
    }

    private static NotesImportedPageResponse ToPageResponse(NotebookItemModel page, string notebookSlug, bool includeContent)
    {
        var contentJsonBytes = GetUtf8ByteCount(page.ContentJson);
        var plainTextLength = page.PlainTextContent?.Length ?? 0;
        var tipTapNodeCount = CountTipTapNodes(page.ContentJson);

        return new NotesImportedPageResponse(
            page.Id,
            page.NotebookId,
            notebookSlug,
            page.ParentId,
            page.Type,
            page.Title,
            page.Path,
            page.ContentFormat ?? "tiptap_json",
            includeContent ? page.ContentJson : null,
            includeContent ? page.PlainTextContent : null,
            includeContent,
            contentJsonBytes,
            plainTextLength,
            tipTapNodeCount,
            page.CreatedAtUtc,
            page.UpdatedAtUtc ?? page.CreatedAtUtc);
    }

    // Delegates to the shared implementation instead of keeping a near-copy. The copy that used to
    // live here differed in two ways that mattered: it serialized without the UnicodeRanges.All
    // encoder, so non-ASCII content was written as \uXXXX escapes, and it did not translate a
    // JsonException from an invalid block into ArgumentException, so that case escaped as a 500
    // instead of the 400 this endpoint's catch produces.
    private static JsonElement AppendBlocks(JsonElement? existingContentJson, JsonElement blocks)
        => TipTapDocumentOperations.AppendBlocks(existingContentJson, blocks);

    private static int GetUtf8ByteCount(JsonElement? contentJson)
        => !contentJson.HasValue || contentJson.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            ? 0
            : Encoding.UTF8.GetByteCount(contentJson.Value.GetRawText());

    private static int CountTipTapNodes(JsonElement? contentJson)
    {
        if (!contentJson.HasValue || contentJson.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return 0;
        }

        return CountTipTapNodes(contentJson.Value);
    }

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

    private static bool TryExtractPagePath(string pathAndAction, string actionSegment, out string pagePath)
    {
        pagePath = string.Empty;
        if (string.IsNullOrWhiteSpace(pathAndAction))
        {
            return false;
        }

        var normalized = pathAndAction.Trim('/');
        var suffix = "/" + actionSegment;
        if (!normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rawPath = normalized[..^suffix.Length];
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return false;
        }

        pagePath = Uri.UnescapeDataString(rawPath);
        return true;
    }

    private static IResult ToNotesError(NotesError error)
        => ToError(error.Code, error.Message, ToStatusCode(error.Kind), error.Field, error.Details);

    private static IResult ToUploadError(UploadError error, int statusCode)
        => ToError(error.Code, error.Message, statusCode);

    private static IResult ToError(
        string code,
        string message,
        int statusCode,
        string? field = null,
        IReadOnlyDictionary<string, object?>? details = null)
    {
        var problem = ApiProblems.Create(code, message, statusCode);
        if (!string.IsNullOrWhiteSpace(field))
        {
            problem.Extensions["field"] = field;
        }

        problem.Extensions["retryable"] = false;
        if (details is not null)
        {
            problem.Extensions["details"] = details;
        }

        return TypedResults.Problem(problem);
    }

    private static int ToStatusCode(NotesFailureKind kind) => NotesFailureStatusCodes.ToStatusCode(kind);

    public sealed record CreateMarkdownPageImportRequest(
        string Title,
        string? ParentPath,
        string UploadId,
        bool IncludeContent = false);

    public sealed record UpdateMarkdownPageImportRequest(
        string UploadId,
        bool IncludeContent = false);

    public sealed record NotesMarkdownUploadResponse(
        string UploadId,
        string? FileName,
        string MediaType,
        int BytesReceived,
        DateTimeOffset ExpiresAtUtc);

    public sealed record NotesDiscardUploadResponse(
        string UploadId,
        string Result);

    public sealed record NotesImportedPageResponse(
        Guid PageId,
        Guid NotebookId,
        string NotebookSlug,
        Guid? ParentId,
        string Type,
        string Title,
        string Path,
        string ContentFormat,
        JsonElement? ContentJson,
        string? PlainTextContent,
        bool ContentIncluded,
        int ContentJsonBytes,
        int PlainTextLength,
        int TipTapNodeCount,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);
}
