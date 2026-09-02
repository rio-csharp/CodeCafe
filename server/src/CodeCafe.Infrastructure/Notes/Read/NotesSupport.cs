using System.Text.Json;
using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using CodeCafe.Domain.Notes.Enums;
using CodeCafe.Domain.Notes.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CodeCafe.Infrastructure.Notes.Read;

internal static class NotesSupport
{
    public static string? SerializeContent(JsonElement? content)
    {
        return
            content is null
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
            item.Slug.Value,
            item.Path.Value,
            item.SortOrder,
            includeContent ? ParseContent(item.ContentJson) : null,
            includeContent ? item.PlainTextContent : null,
            item.IsArchived,
            item.ArchivedAtUtc,
            item.ArchivedByUserId,
            item.CreatedAtUtc,
            item.UpdatedAtUtc
        );
    }

    public static NotebookItemModel ToItemModel(NotebookItemRow row, bool includeContent)
    {
        return new NotebookItemModel(
            row.Id,
            row.NotebookId,
            row.ParentId,
            row.Type.ToString().ToLowerInvariant(),
            row.Title,
            row.Slug.Value,
            row.Path.Value,
            row.SortOrder,
            includeContent ? ParseContent(row.ContentJson) : null,
            includeContent ? row.PlainTextContent : null,
            row.IsArchived,
            row.ArchivedAtUtc,
            row.ArchivedByUserId,
            row.CreatedAtUtc,
            row.UpdatedAtUtc
        );
    }

    public static NotebookDetailModel ToDetailModel(
        Notebook notebook,
        string authorDisplayName,
        NotebookMetadata metadata,
        Guid currentUserId,
        IReadOnlyList<NotebookItemModel> items
    )
    {
        return new NotebookDetailModel(
            notebook.Id,
            notebook.OwnerId,
            notebook.Title,
            notebook.Slug.Value,
            notebook.Description,
            notebook.Visibility.ToString().ToLowerInvariant(),
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
            items
        );
    }

    public static bool IsDuplicateItemPathException(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains(
                "IX_NotebookItems_NotebookId_Path",
                StringComparison.OrdinalIgnoreCase
            )
            || (
                message.Contains("NotebookItems", StringComparison.OrdinalIgnoreCase)
                && message.Contains("Path", StringComparison.OrdinalIgnoreCase)
                && (
                    message.Contains("unique", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                )
            );
    }

    public static bool IsDuplicateNotebookSlugException(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("IX_Notebooks_Slug", StringComparison.OrdinalIgnoreCase)
            || (
                message.Contains("Notebooks", StringComparison.OrdinalIgnoreCase)
                && message.Contains("Slug", StringComparison.OrdinalIgnoreCase)
                && (
                    message.Contains("unique", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                )
            );
    }

    public static bool TryCreateSlug(string value, out NotebookSlug slug)
    {
        slug = null!;
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > NotebookSlug.MaxLength)
        {
            return false;
        }

        slug = NotebookSlug.Create(value);
        return true;
    }

    public static bool TryCreatePath(string value, out NotebookPath path)
    {
        path = null!;
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Trim('/').Length > NotebookPath.MaxLength)
        {
            return false;
        }

        path = NotebookPath.Create(value);
        return true;
    }
}

internal sealed record NotebookItemRow(
    Guid Id,
    Guid NotebookId,
    Guid? ParentId,
    NotebookItemType Type,
    string Title,
    NotebookSlug Slug,
    NotebookPath Path,
    int SortOrder,
    string? ContentJson,
    string? PlainTextContent,
    bool IsArchived,
    DateTimeOffset? ArchivedAtUtc,
    Guid? ArchivedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc
);

internal sealed record NotebookMetadata(
    int ItemCount,
    int FolderCount,
    int PageCount,
    int FavoriteCount,
    bool IsFavoritedByMe,
    DateTimeOffset LastActivityAtUtc
)
{
    public static NotebookMetadata Empty { get; } = new(0, 0, 0, 0, false, DateTimeOffset.MinValue);
}
