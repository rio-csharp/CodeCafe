namespace CodeCafe.Modules.Notes.Application.Notes;

/// <summary>
/// Shared validation for multipart Markdown uploads, used by both the Notes HTTP
/// import endpoints and the MCP HTTP upload endpoints. The two adapters format the
/// returned error differently and use their own wording for the size and media-type
/// messages, so those are passed in by the caller.
/// </summary>
public static class MarkdownUploadValidation
{
    public static readonly string[] SupportedMediaTypes = ["text/markdown"];
    public static readonly string[] SupportedFileExtensions = [".md", ".markdown"];

    public static MarkdownUploadValidationError? Validate(
        bool hasFormContentType,
        MarkdownUploadFile? file,
        string? requestedFileName,
        long maxUploadBytes,
        string fileTooLargeMessage,
        string unsupportedMediaTypeMessage,
        out MarkdownUploadContent? content)
    {
        content = null;

        if (!hasFormContentType)
        {
            return new MarkdownUploadValidationError("invalid_upload_request", "Expected multipart/form-data.", "file");
        }

        if (file is null)
        {
            return new MarkdownUploadValidationError("invalid_upload_request", "Form field 'file' is required.", "file");
        }

        var effectiveFileName = string.IsNullOrWhiteSpace(requestedFileName)
            ? file.FileName
            : requestedFileName.Trim();
        if (string.IsNullOrWhiteSpace(effectiveFileName))
        {
            return new MarkdownUploadValidationError("invalid_upload_file", "A file name is required.", "fileName");
        }

        if (file.Length <= 0)
        {
            return new MarkdownUploadValidationError("invalid_upload_file", "Uploaded file is empty.", "file");
        }

        if (file.Length > maxUploadBytes)
        {
            return new MarkdownUploadValidationError(
                "upload_too_large",
                fileTooLargeMessage,
                "file",
                new Dictionary<string, object?>
                {
                    ["maxUploadBytes"] = maxUploadBytes,
                    ["actualUploadBytes"] = file.Length
                });
        }

        var mediaType = NormalizeMediaType(file.ContentType, effectiveFileName);
        if (!IsSupportedMediaType(mediaType, effectiveFileName))
        {
            return new MarkdownUploadValidationError(
                "unsupported_upload_media_type",
                unsupportedMediaTypeMessage,
                "file",
                new Dictionary<string, object?>
                {
                    ["supportedMediaTypes"] = SupportedMediaTypes,
                    ["supportedFileExtensions"] = SupportedFileExtensions,
                    ["receivedMediaType"] = file.ContentType
                });
        }

        content = new MarkdownUploadContent(effectiveFileName, mediaType);
        return null;
    }

    public static string NormalizeMediaType(string? contentType, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(contentType)
            && !string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return contentType.Trim();
        }

        return HasSupportedFileExtension(fileName) ? "text/markdown" : "text/plain";
    }

    private static bool IsSupportedMediaType(string mediaType, string fileName)
        => SupportedMediaTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase)
           || HasSupportedFileExtension(fileName);

    private static bool HasSupportedFileExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return SupportedFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}

public sealed record MarkdownUploadFile(string? ContentType, string? FileName, long Length);

public sealed record MarkdownUploadContent(string FileName, string MediaType);

public sealed record MarkdownUploadValidationError(
    string Code,
    string Message,
    string Field,
    IReadOnlyDictionary<string, object?>? Details = null);
