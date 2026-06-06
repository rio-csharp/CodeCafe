using CodeCafe.Api.Errors;
using CodeCafe.Application.Notes;
using CodeCafe.Mcp.Configuration;
using CodeCafe.Mcp.Tools.Notes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodeCafe.Server.Endpoints.Notes;

public static class NotesMarkdownImportEndpoints
{
    private static readonly string[] SupportedMarkdownMediaTypes = ["text/markdown"];
    private static readonly string[] SupportedMarkdownExtensions = [".md", ".markdown"];

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
        HttpContext httpContext,
        HttpRequest request,
        IMcpUploadStore uploadStore,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var actorId = GetCurrentUserId(httpContext.User);
        if (actorId == Guid.Empty)
        {
            return ToError("authenticated_actor_required", "The Notes API requires an authenticated CodeCafe user.", StatusCodes.Status401Unauthorized);
        }

        if (!request.HasFormContentType)
        {
            return ToError("invalid_upload_request", "Expected multipart/form-data.", StatusCodes.Status400BadRequest, field: "file");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null)
        {
            return ToError("invalid_upload_request", "Form field 'file' is required.", StatusCodes.Status400BadRequest, field: "file");
        }

        var effectiveFileName = string.IsNullOrWhiteSpace(form["fileName"])
            ? file.FileName
            : form["fileName"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(effectiveFileName))
        {
            return ToError("invalid_upload_file", "A file name is required.", StatusCodes.Status400BadRequest, field: "fileName");
        }

        if (file.Length <= 0)
        {
            return ToError("invalid_upload_file", "Uploaded file is empty.", StatusCodes.Status400BadRequest, field: "file");
        }

        var options = mcpOptionsAccessor.Value;
        if (file.Length > options.MaxUploadBytes)
        {
            return ToError(
                "upload_too_large",
                "Upload exceeds maxUploadBytes.",
                StatusCodes.Status400BadRequest,
                field: "file",
                details: new Dictionary<string, object?>
                {
                    ["maxUploadBytes"] = options.MaxUploadBytes,
                    ["actualUploadBytes"] = file.Length
                });
        }

        var mediaType = NormalizeMediaType(file.ContentType, effectiveFileName);
        if (!IsSupportedMarkdownUpload(mediaType, effectiveFileName))
        {
            return ToError(
                "unsupported_upload_media_type",
                "Only Markdown uploads are supported.",
                StatusCodes.Status400BadRequest,
                field: "file",
                details: new Dictionary<string, object?>
                {
                    ["supportedMediaTypes"] = SupportedMarkdownMediaTypes,
                    ["supportedFileExtensions"] = SupportedMarkdownExtensions,
                    ["receivedMediaType"] = file.ContentType
                });
        }

        string contentText;
        await using (var stream = file.OpenReadStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
        {
            contentText = await reader.ReadToEndAsync(cancellationToken);
        }

        var uploadResult = await uploadStore.CreateTextAsync(
            actorId,
            effectiveFileName,
            mediaType,
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
        HttpContext httpContext,
        IMcpUploadStore uploadStore,
        CancellationToken cancellationToken)
    {
        var actorId = GetCurrentUserId(httpContext.User);
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
        HttpContext httpContext,
        INotebookReadService notebookReadService,
        INotebookItemMutationService notebookItemMutationService,
        IMcpContentImportService contentImportService,
        CancellationToken cancellationToken)
    {
        var actorId = GetCurrentUserId(httpContext.User);
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

        var createResult = await notebookItemMutationService.CreateNotebookItemAsync(
            notebookResult.Value!.Id,
            actorId,
            parentResult.Value?.Id,
            "page",
            request.Title,
            sortOrder: 0,
            contentResult.Value,
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
        HttpContext httpContext,
        INotebookReadService notebookReadService,
        INotebookItemMutationService notebookItemMutationService,
        IMcpContentImportService contentImportService,
        CancellationToken cancellationToken)
    {
        if (!TryExtractPagePath(pathAndAction, "import-markdown", out var path))
        {
            return Results.NotFound();
        }

        var actorId = GetCurrentUserId(httpContext.User);
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

        var updateResult = await notebookItemMutationService.UpdateNotebookItemAsync(
            pageResult.Value!.NotebookId,
            pageResult.Value.Id,
            actorId,
            pageResult.Value.Title,
            default,
            sortOrder: null,
            contentResult.Value,
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
        HttpContext httpContext,
        INotebookReadService notebookReadService,
        INotebookItemMutationService notebookItemMutationService,
        IMcpContentImportService contentImportService,
        CancellationToken cancellationToken)
    {
        if (!TryExtractPagePath(pathAndAction, "append-markdown", out var path))
        {
            return Results.NotFound();
        }

        var actorId = GetCurrentUserId(httpContext.User);
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

        var updateResult = await notebookItemMutationService.UpdateNotebookItemAsync(
            pageResult.Value.NotebookId,
            pageResult.Value.Id,
            actorId,
            pageResult.Value.Title,
            default,
            sortOrder: null,
            nextContentJson,
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

    private static JsonElement AppendBlocks(JsonElement? existingContentJson, JsonElement blocks)
    {
        if (blocks.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("Blocks must be a JSON array.");
        }

        var root = existingContentJson is null || existingContentJson.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            ? new JsonObject
            {
                ["type"] = "doc",
                ["content"] = new JsonArray()
            }
            : JsonNode.Parse(existingContentJson.Value.GetRawText())?.AsObject()
              ?? new JsonObject
              {
                  ["type"] = "doc",
                  ["content"] = new JsonArray()
              };

        root["type"] ??= "doc";
        var content = root["content"] as JsonArray ?? new JsonArray();
        root["content"] = content;

        foreach (var block in blocks.EnumerateArray())
        {
            content.Add(JsonNode.Parse(block.GetRawText()));
        }

        return JsonSerializer.SerializeToElement(root);
    }

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

    private static string NormalizeMediaType(string? contentType, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(contentType)
            && !string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return contentType.Trim();
        }

        return HasSupportedMarkdownExtension(fileName) ? "text/markdown" : "text/plain";
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

    private static bool IsSupportedMarkdownUpload(string mediaType, string fileName)
        => SupportedMarkdownMediaTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase)
           || HasSupportedMarkdownExtension(fileName);

    private static bool HasSupportedMarkdownExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return SupportedMarkdownExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
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

    private static IResult ToUploadError(NotesUploadError error, int statusCode)
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

    private static int ToStatusCode(NotesFailureKind kind)
        => kind switch
        {
            NotesFailureKind.Validation => StatusCodes.Status400BadRequest,
            NotesFailureKind.Forbidden => StatusCodes.Status403Forbidden,
            NotesFailureKind.NotFound => StatusCodes.Status404NotFound,
            NotesFailureKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

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
