using System.Text.Json;

namespace CodeCafe.WebApi.Mcp;

public sealed record NotebookSearchResultResponse(
    Guid NotebookId,
    string NotebookSlug,
    string NotebookTitle,
    Guid? ItemId,
    string? Path,
    string? ItemTitle,
    string ResultType,
    string? PlainTextSnippet,
    bool CanEdit,
    DateTimeOffset? UpdatedAtUtc);

public sealed record SearchNotesToolResponse(
    string Query,
    int TotalCount,
    IReadOnlyList<NotebookSearchResultResponse> Results);

public sealed record ListNotebookItemsToolResponse(
    Guid NotebookId,
    string NotebookSlug,
    string NotebookTitle,
    bool CanEdit,
    IReadOnlyList<NotebookItemToolResponse> Items);

public sealed record NotebookItemToolResponse(
    Guid Id,
    Guid NotebookId,
    Guid? ParentId,
    string Type,
    string Title,
    string Slug,
    string Path,
    int SortOrder,
    string? ContentFormat,
    JsonElement? ContentJson,
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
    DateTimeOffset? PublishedAtUtc);

public sealed record GetPageToolResponse(
    Guid PageId,
    Guid NotebookId,
    string NotebookSlug,
    string Title,
    string Path,
    string ContentFormat,
    JsonElement? ContentJson,
    string? PlainTextContent,
    bool CanEdit,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record CreatePageToolResponse(
    Guid PageId,
    Guid NotebookId,
    string NotebookSlug,
    string Title,
    string Path,
    Guid? ParentId,
    int SortOrder,
    string? ContentFormat,
    JsonElement? ContentJson,
    string? PlainTextContent,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record UpdatePageContentToolResponse(
    Guid PageId,
    Guid NotebookId,
    string NotebookSlug,
    string Title,
    string Path,
    string? ContentFormat,
    JsonElement? ContentJson,
    string? PlainTextContent,
    DateTimeOffset? UpdatedAtUtc);

public sealed record MoveItemToolResponse(
    Guid ItemId,
    Guid NotebookId,
    string NotebookSlug,
    string Title,
    string Type,
    string Path,
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

public sealed record ReorderNotesItemRequest(
    string Path,
    string? ParentPath,
    int SortOrder);

public sealed record McpToolErrorResponse(
    string Code,
    string Message,
    bool Retryable);
