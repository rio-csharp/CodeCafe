namespace CodeCafe.Mcp.Tools.Notes;

public sealed record NotebookSearchResultResponse(
    Guid NotebookId,
    string NotebookSlug,
    string NotebookTitle,
    string NotebookUri,
    string ItemsUri,
    Guid? ItemId,
    string? Path,
    string? ItemTitle,
    string ResultType,
    string? ResourceUri,
    string? PlainTextSnippet,
    bool CanEdit,
    DateTimeOffset? UpdatedAtUtc);

public sealed record SearchNotesToolResponse(
    string Query,
    int TotalCount,
    IReadOnlyList<NotebookSearchResultResponse> Results);

public sealed record ListNotebooksToolResponse(
    string Scope,
    int TotalCount,
    IReadOnlyList<GetNotebookToolResponse> Notebooks);

public sealed record ListNotebookItemsToolResponse(
    Guid NotebookId,
    string NotebookSlug,
    string NotebookTitle,
    bool CanEdit,
    int TotalCount,
    int Offset,
    int ReturnedCount,
    IReadOnlyList<NotebookItemToolResponse> Items);

public sealed record NotebookItemToolResponse(
    Guid Id,
    Guid NotebookId,
    string NotebookSlug,
    Guid? ParentId,
    string Type,
    string Title,
    string Slug,
    string Path,
    string? ResourceUri,
    int SortOrder,
    string? ContentFormat,
    string? ContentJson,
    string? PlainTextContent,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record GetNotebookToolResponse(
    Guid Id,
    Guid OwnerId,
    string Slug,
    string Title,
    string? Description,
    string Visibility,
    bool IsPublished,
    string AuthorDisplayName,
    bool CanEdit,
    int ItemCount,
    int FolderCount,
    int PageCount,
    int FavoriteCount,
    bool IsFavoritedByMe,
    DateTimeOffset LastActivityAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? PublishedAtUtc,
    string NotebookUri,
    string ItemsUri);

public sealed record GetPageToolResponse(
    Guid PageId,
    Guid NotebookId,
    string NotebookSlug,
    string Title,
    string Path,
    string NotebookUri,
    string PageUri,
    string ContentFormat,
    string? ContentJson,
    string? PlainTextContent,
    int ContentJsonBytes,
    int PlainTextLength,
    int TipTapNodeCount,
    bool CanEdit,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record CreatePageToolResponse(
    Guid PageId,
    Guid NotebookId,
    string NotebookSlug,
    string Title,
    string Path,
    string NotebookUri,
    string PageUri,
    Guid? ParentId,
    int SortOrder,
    string? ContentFormat,
    string? ContentJson,
    string? PlainTextContent,
    bool ContentIncluded,
    int ContentJsonBytes,
    int PlainTextLength,
    int TipTapNodeCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record CreateItemToolResponse(
    Guid ItemId,
    Guid NotebookId,
    string NotebookSlug,
    string Title,
    string Type,
    string Path,
    string NotebookUri,
    string ItemsUri,
    string? ResourceUri,
    Guid? ParentId,
    int SortOrder,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record UpdatePageContentToolResponse(
    Guid PageId,
    Guid NotebookId,
    string NotebookSlug,
    string Title,
    string Path,
    string NotebookUri,
    string PageUri,
    string? ContentFormat,
    string? ContentJson,
    string? PlainTextContent,
    bool ContentIncluded,
    int ContentJsonBytes,
    int PlainTextLength,
    int TipTapNodeCount,
    DateTimeOffset? UpdatedAtUtc);

public sealed record MoveItemToolResponse(
    Guid ItemId,
    Guid NotebookId,
    string NotebookSlug,
    string Title,
    string Type,
    string Path,
    string NotebookUri,
    string ItemsUri,
    string? ResourceUri,
    Guid? ParentId,
    int SortOrder,
    DateTimeOffset? UpdatedAtUtc);

public sealed record ReorderItemsToolResponse(
    Guid NotebookId,
    string NotebookSlug,
    IReadOnlyList<NotebookItemToolResponse> Items);

public sealed record DeleteItemToolResponse(
    Guid NotebookId,
    string NotebookSlug,
    Guid ItemId,
    string Path,
    string Result);

public sealed record DeleteNotebookToolResponse(
    Guid NotebookId,
    string NotebookSlug,
    string Result);

public sealed record GetNotesLimitsToolResponse(
    int MaxInlineContentBytes,
    int MaxUploadChunkBytes,
    int MaxUploadBytes,
    int MaxHttpUploadBytes,
    int HttpUploadIdleTimeoutSeconds,
    int MaxPageContentBytes,
    int MaxListItemsLimit,
    int MaxTipTapDepth,
    int MaxTipTapNodeCount,
    int MaxTipTapTextLength,
    IReadOnlyList<string> SupportedImportFormats,
    IReadOnlyList<string> SupportedHttpUploadMediaTypes);

public sealed record CreateUploadToolResponse(
    string UploadId,
    string? FileName,
    string MediaType,
    int BytesReceived,
    DateTimeOffset CreatedAtUtc);

public sealed record AppendUploadChunkToolResponse(
    string UploadId,
    int BytesReceived,
    int ChunkBytesReceived,
    bool IsReady);

public sealed record DiscardUploadToolResponse(
    string UploadId,
    string Result);

public sealed record ReorderNotesItemRequest(
    string Path,
    string? ParentPath,
    int SortOrder);

public sealed record McpToolErrorResponse(
    string Code,
    string Message,
    string? Field,
    bool Retryable,
    string? Suggestion,
    IReadOnlyDictionary<string, object?>? Details);
