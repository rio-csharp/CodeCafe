using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace CodeCafe.Api.Endpoints.Notes;

public sealed record NotebookSummaryResponse(
    Guid Id,
    Guid OwnerId,
    string Title,
    string Slug,
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

public sealed record NotebookDetailResponse(
    Guid Id,
    Guid OwnerId,
    string Title,
    string Slug,
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
    IReadOnlyList<NotebookItemResponse> Items);

public sealed record NotebookItemResponse(
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
    bool IsArchived,
    DateTimeOffset? ArchivedAtUtc,
    Guid? ArchivedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record NotebookFavoriteResponse(
    Guid NotebookId,
    bool IsFavorited,
    int FavoriteCount);

public sealed record CreateNotebookRequest(
    [Required, StringLength(160, MinimumLength = 1)] string Title,
    [StringLength(1000)] string? Description,
    string? Visibility);

public sealed record UpdateNotebookRequest(
    [Required, StringLength(160, MinimumLength = 1)] string Title,
    [StringLength(1000)] string? Description,
    string? Visibility);

public sealed record CreateNotebookItemRequest(
    Guid? ParentId,
    [Required] string Type,
    [Required, StringLength(160, MinimumLength = 1)] string Title,
    int SortOrder,
    JsonElement? ContentJson);

public sealed record UpdateNotebookItemRequest(
    [Required, StringLength(160, MinimumLength = 1)] string Title,
    JsonElement ParentId,
    int? SortOrder,
    JsonElement ContentJson,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record ReorderNotebookItemsRequest(
    [Required] IReadOnlyList<ReorderNotebookItemRequest> Items);

public sealed record ReorderNotebookItemRequest(
    Guid ItemId,
    Guid? ParentId,
    int SortOrder);

public sealed record ReorderNotebookItemsResponse(
    IReadOnlyList<NotebookItemResponse> Items);
