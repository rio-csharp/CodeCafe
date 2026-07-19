using System.Text.Json;

namespace CodeCafe.Modules.Notes.Application.Notes;

public interface IMcpContentImportService
{
    Task<NotesResult<JsonElement?>> ResolveOptionalPageContentAsync(
        Guid actorId,
        JsonElement? inlineContentJson,
        string? contentUploadId,
        string? contentFormat,
        string errorCode,
        string invalidMessage,
        CancellationToken cancellationToken);

    Task<NotesResult<JsonElement>> ResolveRequiredPageContentAsync(
        Guid actorId,
        JsonElement? inlineContentJson,
        string? contentUploadId,
        string? contentFormat,
        string errorCode,
        string invalidMessage,
        CancellationToken cancellationToken);

    Task<NotesResult<JsonElement>> ResolveRequiredBlocksAsync(
        Guid actorId,
        JsonElement? inlineBlocks,
        string? blocksUploadId,
        string? blocksFormat,
        string errorCode,
        string invalidMessage,
        CancellationToken cancellationToken);

    NotesResult EnforcePageContentSize(JsonElement contentJson, string errorCode);

    Task DeleteUploadAsync(Guid actorId, string? uploadId, CancellationToken cancellationToken);
}
