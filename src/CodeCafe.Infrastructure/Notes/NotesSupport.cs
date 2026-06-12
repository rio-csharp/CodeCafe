using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CodeCafe.Infrastructure.Notes;

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
        Guid currentUserId)
    {
        var items = notebook.Items
            .OrderBy(item => item.ParentId)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Title)
            .Select(ToItemModel)
            .ToList();

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
