using CodeCafe.Modules.Mcp.Tools.Notes;
using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Shared.Application.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;
using System.Text;

namespace CodeCafe.Modules.Mcp.Common;

public static class McpHttpUploadEndpoints
{
    private static readonly string[] SupportedMarkdownMediaTypes = ["text/markdown"];
    private static readonly string[] SupportedMarkdownExtensions = [".md", ".markdown"];

    public static IEndpointRouteBuilder MapMcpHttpUploadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/mcp/uploads")
            .WithTags("MCP Uploads")
            .RequireRateLimiting("mcp")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme
            })
            .DisableAntiforgery();

        group.MapPost("/markdown", UploadMarkdownAsync);
        group.MapDelete("/{uploadId}", DeleteUploadAsync);

        return endpoints;
    }

    private static async Task<IResult> UploadMarkdownAsync(
        HttpContext httpContext,
        HttpRequest request,
        IMcpUploadStore uploadStore,
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var options = mcpOptionsAccessor.Value;
        var actorResult = NotesMcpSupport.RequireActor(httpContext.User, options.RequiredWriteScopes);
        if (!actorResult.Succeeded)
        {
            return ToProblemResult(actorResult.Error!);
        }

        if (!request.HasFormContentType)
        {
            return ToUploadError("invalid_upload_request", "Expected multipart/form-data.", "file", StatusCodes.Status400BadRequest);
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null)
        {
            return ToUploadError("invalid_upload_request", "Form field 'file' is required.", "file", StatusCodes.Status400BadRequest);
        }

        var effectiveFileName = string.IsNullOrWhiteSpace(form["fileName"])
            ? file.FileName
            : form["fileName"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(effectiveFileName))
        {
            return ToUploadError("invalid_upload_file", "A file name is required.", "fileName", StatusCodes.Status400BadRequest);
        }

        if (file.Length <= 0)
        {
            return ToUploadError("invalid_upload_file", "Uploaded file is empty.", "file", StatusCodes.Status400BadRequest);
        }

        if (file.Length > options.MaxUploadBytes)
        {
            return ToUploadError(
                "upload_too_large",
                $"Upload exceeds the limit of {options.MaxUploadBytes} bytes.",
                "file",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, object?>
                {
                    ["maxUploadBytes"] = options.MaxUploadBytes,
                    ["actualUploadBytes"] = file.Length
                });
        }

        var mediaType = NormalizeMediaType(file.ContentType, effectiveFileName);
        if (!IsSupportedMarkdownUpload(mediaType, effectiveFileName))
        {
            return ToUploadError(
                "unsupported_upload_media_type",
                "Only Markdown text uploads are supported for this endpoint.",
                "file",
                StatusCodes.Status400BadRequest,
                new Dictionary<string, object?>
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

        var createResult = await uploadStore.CreateTextAsync(
            actorResult.Value,
            effectiveFileName,
            mediaType,
            contentText,
            options.MaxUploadBytes,
            cancellationToken);
        if (!createResult.Succeeded)
        {
            return ToUploadError(
                createResult.Error!.Code,
                createResult.Error.Message,
                "file",
                StatusCodes.Status400BadRequest);
        }

        var upload = createResult.Value!;
        return TypedResults.Ok(new McpHttpUploadResponse(
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
        IOptions<McpOptions> mcpOptionsAccessor,
        CancellationToken cancellationToken)
    {
        var options = mcpOptionsAccessor.Value;
        var actorResult = NotesMcpSupport.RequireActor(httpContext.User, options.RequiredWriteScopes);
        if (!actorResult.Succeeded)
        {
            return ToProblemResult(actorResult.Error!);
        }

        var removed = await uploadStore.DeleteAsync(actorResult.Value, uploadId, cancellationToken);
        return TypedResults.Ok(new McpHttpDiscardUploadResponse(uploadId, removed ? "discarded" : "already_absent"));
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

    private static bool IsSupportedMarkdownUpload(string mediaType, string fileName)
        => SupportedMarkdownMediaTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase)
           || HasSupportedMarkdownExtension(fileName);

    private static bool HasSupportedMarkdownExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return SupportedMarkdownExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static IResult ToProblemResult(NotesError error)
    {
        var statusCode = error.Kind switch
        {
            NotesFailureKind.Forbidden => StatusCodes.Status403Forbidden,
            NotesFailureKind.NotFound => StatusCodes.Status404NotFound,
            NotesFailureKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return ToUploadError(error.Code, error.Message, error.Field, statusCode, error.Details);
    }

    private static IResult ToUploadError(
        string code,
        string message,
        string? field,
        int statusCode,
        IReadOnlyDictionary<string, object?>? details = null)
    {
        return TypedResults.Json(
            new McpHttpUploadErrorResponse(code, message, field, Retryable: false, details),
            statusCode: statusCode);
    }

    private sealed record McpHttpUploadResponse(
        string UploadId,
        string? FileName,
        string MediaType,
        int BytesReceived,
        DateTimeOffset ExpiresAtUtc);

    private sealed record McpHttpDiscardUploadResponse(
        string UploadId,
        string Result);

    private sealed record McpHttpUploadErrorResponse(
        string Code,
        string Message,
        string? Field,
        bool Retryable,
        IReadOnlyDictionary<string, object?>? Details);
}
