using CodeCafe.Host.Mcp;
using CodeCafe.Application.Notes;
using CodeCafe.Application.Common.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;
using System.Text;

namespace CodeCafe.Host.Mcp;

public static class McpHttpUploadEndpoints
{
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

        IFormFile? file = null;
        string? requestedFileName = null;
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync(cancellationToken);
            file = form.Files.GetFile("file");
            requestedFileName = form["fileName"].ToString();
        }

        var validationError = MarkdownUploadValidation.Validate(
            request.HasFormContentType,
            file is null ? null : new MarkdownUploadFile(file.ContentType, file.FileName, file.Length),
            requestedFileName,
            options.MaxUploadBytes,
            $"Upload exceeds the limit of {options.MaxUploadBytes} bytes.",
            "Only Markdown text uploads are supported for this endpoint.",
            out var validatedUpload);
        if (validationError is not null)
        {
            return ToUploadError(
                validationError.Code,
                validationError.Message,
                validationError.Field,
                StatusCodes.Status400BadRequest,
                validationError.Details);
        }

        string contentText;
        await using (var stream = file!.OpenReadStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
        {
            contentText = await reader.ReadToEndAsync(cancellationToken);
        }

        var createResult = await uploadStore.CreateTextAsync(
            actorResult.Value,
            validatedUpload!.FileName,
            validatedUpload.MediaType,
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
