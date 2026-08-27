using System.Text.Json;
using CodeCafe.Application.Notes;

namespace CodeCafe.Application.Common.Uploads;

/// <summary>
/// Converts uploaded content (markdown, plain text) into structured formats (TipTap JSON).
/// Handles both inline content and upload-based content, with validation and size enforcement.
/// </summary>
public interface IContentImporter
{
    Task<NotesResult<JsonElement?>> ResolveOptionalPageContentAsync(
        Guid actorId,
        JsonElement? inlineContentJson,
        string? contentUploadId,
        string? contentFormat,
        string errorCode,
        string invalidMessage,
        CancellationToken cancellationToken
    );

    Task<NotesResult<JsonElement>> ResolveRequiredPageContentAsync(
        Guid actorId,
        JsonElement? inlineContentJson,
        string? contentUploadId,
        string? contentFormat,
        string errorCode,
        string invalidMessage,
        CancellationToken cancellationToken
    );

    Task<NotesResult<JsonElement>> ResolveRequiredBlocksAsync(
        Guid actorId,
        JsonElement? inlineBlocks,
        string? blocksUploadId,
        string? blocksFormat,
        string errorCode,
        string invalidMessage,
        CancellationToken cancellationToken
    );

    NotesResult EnforcePageContentSize(JsonElement contentJson, string errorCode);

    Task DeleteUploadAsync(Guid actorId, string? uploadId, CancellationToken cancellationToken);
}
