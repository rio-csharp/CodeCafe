using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Modules.Notes.Domain.Notes;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CodeCafe.Modules.Notes.Infrastructure.Notes;

internal static class NotesSupport
{
    public const string PageContentFormat = "tiptap_json";

    public static string? SerializeContent(JsonElement? content)
    {
        return content is null
               || content.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            ? null
            : content.Value.GetRawText();
    }

    public static JsonElement? ParseContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        using var document = JsonDocument.Parse(content);
        return document.RootElement.Clone();
    }

    public static string GetAuthorDisplayName(IReadOnlyDictionary<Guid, string> displayNames, Guid ownerId)
    {
        return displayNames.TryGetValue(ownerId, out var displayName) ? displayName : "Unknown";
    }

    public static NotebookItemModel ToItemModel(NotebookItem item)
    {
        return ToItemModel(item, includeContent: true);
    }

    public static NotebookItemModel ToItemModel(NotebookItem item, bool includeContent)
    {
        return new NotebookItemModel(
            item.Id,
            item.NotebookId,
            item.ParentId,
            item.Type.ToString().ToLowerInvariant(),
            item.Title,
            item.Slug,
            item.Path,
            item.SortOrder,
            item.ContentFormat,
            includeContent ? ParseContent(item.ContentJson) : null,
            includeContent ? item.PlainTextContent : null,
            item.IsArchived,
            item.ArchivedAtUtc,
            item.ArchivedByUserId,
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
    }

    public static NotebookItemModel ToItemModel(NotebookItemRow row, bool includeContent)
    {
        return new NotebookItemModel(
            row.Id,
            row.NotebookId,
            row.ParentId,
            row.Type.ToString().ToLowerInvariant(),
            row.Title,
            row.Slug,
            row.Path,
            row.SortOrder,
            row.ContentFormat,
            includeContent ? ParseContent(row.ContentJson) : null,
            includeContent ? row.PlainTextContent : null,
            row.IsArchived,
            row.ArchivedAtUtc,
            row.ArchivedByUserId,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);
    }

    public static NotebookSummaryModel ToSummaryModel(
        Notebook notebook,
        string authorDisplayName,
        NotebookMetadata metadata,
        Guid currentUserId)
    {
        return new NotebookSummaryModel(
            notebook.Id,
            notebook.OwnerId,
            notebook.Title,
            notebook.Slug,
            notebook.Description,
            notebook.Visibility.ToString().ToLowerInvariant(),
            notebook.IsPublished,
            authorDisplayName,
            notebook.OwnerId == currentUserId,
            metadata.ItemCount,
            metadata.FolderCount,
            metadata.PageCount,
            metadata.FavoriteCount,
            metadata.IsFavoritedByMe,
            metadata.LastActivityAtUtc,
            notebook.CreatedAtUtc,
            notebook.UpdatedAtUtc,
            notebook.PublishedAtUtc);
    }

    public static NotebookDetailModel ToDetailModel(
        Notebook notebook,
        string authorDisplayName,
        NotebookMetadata metadata,
        Guid currentUserId,
        IReadOnlyList<NotebookItemModel> items)
    {
        return new NotebookDetailModel(
            notebook.Id,
            notebook.OwnerId,
            notebook.Title,
            notebook.Slug,
            notebook.Description,
            notebook.Visibility.ToString().ToLowerInvariant(),
            notebook.IsPublished,
            authorDisplayName,
            notebook.OwnerId == currentUserId,
            metadata.ItemCount,
            metadata.FolderCount,
            metadata.PageCount,
            metadata.FavoriteCount,
            metadata.IsFavoritedByMe,
            metadata.LastActivityAtUtc,
            notebook.CreatedAtUtc,
            notebook.UpdatedAtUtc,
            notebook.PublishedAtUtc,
            items);
    }

    public static bool IsDuplicateFavoriteException(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("NotebookFavorites", StringComparison.OrdinalIgnoreCase)
            && (message.Contains("unique", StringComparison.OrdinalIgnoreCase)
                || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Detects a violation of the unique (NotebookId, Path) index on NotebookItems, which is how a
    /// concurrent create/rename of the same title surfaces.
    /// </summary>
    public static bool IsDuplicateItemPathException(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("IX_NotebookItems_NotebookId_Path", StringComparison.OrdinalIgnoreCase)
               || (message.Contains("NotebookItems", StringComparison.OrdinalIgnoreCase)
                   && message.Contains("Path", StringComparison.OrdinalIgnoreCase)
                   && (message.Contains("unique", StringComparison.OrdinalIgnoreCase)
                       || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)));
    }

    public static bool IsDuplicateNotebookSlugException(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("IX_Notebooks_Slug", StringComparison.OrdinalIgnoreCase)
               || (message.Contains("Notebooks", StringComparison.OrdinalIgnoreCase)
                   && message.Contains("Slug", StringComparison.OrdinalIgnoreCase)
                   && (message.Contains("unique", StringComparison.OrdinalIgnoreCase)
                       || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)));
    }
}

internal sealed record NotebookItemRow(
    Guid Id,
    Guid NotebookId,
    Guid? ParentId,
    NotebookItemType Type,
    string Title,
    string Slug,
    string Path,
    int SortOrder,
    string? ContentFormat,
    string? ContentJson,
    string? PlainTextContent,
    bool IsArchived,
    DateTimeOffset? ArchivedAtUtc,
    Guid? ArchivedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

internal sealed record NotebookMetadata(
    int ItemCount,
    int FolderCount,
    int PageCount,
    int FavoriteCount,
    bool IsFavoritedByMe,
    DateTimeOffset LastActivityAtUtc)
{
    public static NotebookMetadata Empty { get; } = new(0, 0, 0, 0, false, DateTimeOffset.MinValue);
}
