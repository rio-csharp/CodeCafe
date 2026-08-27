using System.Text.Json;

namespace CodeCafe.Application.Notes;

public sealed record NotebookSummaryModel(
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
    DateTimeOffset? PublishedAtUtc
);

public sealed record NotebookDetailModel(
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
    IReadOnlyList<NotebookItemModel> Items
);

public sealed record NotebookFavoriteModel(Guid NotebookId, bool IsFavorited, int FavoriteCount);

/// <summary>
/// The AI context projection plus the full active-page item, both derived from a single notebook
/// load. <paramref name="ActivePageFound"/> is false only when an active page was requested and no
/// item matched, letting the caller distinguish that from "no active page requested".
/// </summary>
public sealed record NotebookContextWithItem(
    NotebookContextModel Context,
    NotebookItemModel? ActivePage,
    bool ActivePageFound
);

public sealed record NotebookItemModel(
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
    DateTimeOffset? UpdatedAtUtc
);

public sealed record NotebookItemSearchModel(
    Guid NotebookId,
    string NotebookSlug,
    string NotebookTitle,
    bool NotebookCanEdit,
    Guid ItemId,
    string Path,
    string Title,
    string Type,
    string? PlainTextContent,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc
);

public sealed record NotebookItemsPageModel(int TotalCount, IReadOnlyList<NotebookItemModel> Items);

public sealed record NotebookContextItemModel(
    Guid Id,
    Guid? ParentId,
    string Type,
    string Title,
    string Path,
    int SortOrder,
    string? TextPreview
);

public sealed record NotebookContextModel(
    Guid Id,
    Guid OwnerId,
    string Title,
    string Slug,
    string? Description,
    bool CanEdit,
    IReadOnlyList<NotebookContextItemModel> Items
)
{
    public const int TextPreviewChars = 1000;
}

public sealed record ReorderNotebookItemModel(Guid ItemId, Guid? ParentId, int SortOrder);
