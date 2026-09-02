using System.Text.Json;

namespace CodeCafe.Application.Notes;

public sealed record NotebookSummaryModel(
    Guid Id,
    Guid OwnerId,
    string Title,
    string Slug,
    string? Description,
    string Visibility,
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

public sealed record NotebookContextWithItem(
    NotebookContextModel Context,
    NotebookItemModel? ActivePage,
    // False only when an active page was requested but no item matched, distinguishing that
    // from "no active page requested".
    bool ActivePageResolved
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
    string? Snippet,
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
