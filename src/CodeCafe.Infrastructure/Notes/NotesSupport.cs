using CodeCafe.Application.Notes;
using CodeCafe.Domain.Notes;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace CodeCafe.Infrastructure.Notes;

internal static class NotesSupport
{
    public const string PageContentFormat = "tiptap_json";

    public static bool CanReadNotebook(Notebook notebook, Guid currentUserId)
    {
        if (notebook.OwnerId == currentUserId)
        {
            return true;
        }

        if (notebook.Visibility == NotebookVisibility.Unlisted)
        {
            return true;
        }

        return notebook.Visibility == NotebookVisibility.Public && notebook.IsPublished;
    }

    public static string? NormalizeSearch(string? search)
    {
        var trimmed = search?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : $"%{trimmed}%";
    }

    public static string? NormalizeOptionalText(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    public static string NormalizePath(string path)
    {
        return path.Trim().Trim('/');
    }

    public static bool TryParseVisibility(string? value, out NotebookVisibility visibility)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            visibility = NotebookVisibility.Private;
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out visibility);
    }

    public static bool TryParseItemType(string value, out NotebookItemType type)
    {
        return Enum.TryParse(value, ignoreCase: true, out type);
    }

    public static bool TryParseOptionalGuid(JsonElement value, out Guid? guid)
    {
        guid = null;

        if (value.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var rawValue = value.GetString();
        if (!Guid.TryParse(rawValue, out var parsedGuid))
        {
            return false;
        }

        guid = parsedGuid;
        return true;
    }

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

    public static NotebookItem? ValidateRequestedParent(
        IReadOnlyList<NotebookItem> notebookItems,
        Guid itemId,
        Guid? parentId)
    {
        if (parentId is null)
        {
            return null;
        }

        return notebookItems.SingleOrDefault(item => item.Id == parentId && item.Id != itemId);
    }

    public static bool WouldCreateCycle(
        IReadOnlyList<NotebookItem> notebookItems,
        Guid itemId,
        Guid proposedParentId,
        IReadOnlyList<ReorderNotebookItemModel>? reorderItems = null)
    {
        var parentMap = notebookItems.ToDictionary(item => item.Id, item => item.ParentId);
        if (reorderItems is not null)
        {
            foreach (var reorderItem in reorderItems)
            {
                parentMap[reorderItem.ItemId] = reorderItem.ParentId;
            }
        }

        Guid? currentParentId = proposedParentId;
        while (currentParentId is not null)
        {
            if (currentParentId == itemId)
            {
                return true;
            }

            currentParentId = parentMap.GetValueOrDefault(currentParentId.Value);
        }

        return false;
    }

    public static string GenerateItemPath(
        IReadOnlyList<NotebookItem> notebookItems,
        string? parentPath,
        string title,
        Guid currentItemId)
    {
        var baseSlug = SlugGenerator.FromTitle(title, "page");
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var slug = SlugGenerator.WithSuffix(baseSlug, attempt);
            var path = string.IsNullOrWhiteSpace(parentPath) ? slug : $"{parentPath}/{slug}";
            var exists = notebookItems.Any(item => item.Id != currentItemId && item.Path == path);
            if (!exists)
            {
                return path;
            }
        }

        var finalSlug = $"{baseSlug}-{Guid.NewGuid():N}";
        return string.IsNullOrWhiteSpace(parentPath) ? finalSlug : $"{parentPath}/{finalSlug}";
    }

    public static void ApplyDescendantPathUpdate(
        IReadOnlyList<NotebookItem> notebookItems,
        Guid itemId,
        string oldPath,
        string newPath)
    {
        foreach (var descendant in notebookItems.Where(item =>
                     item.Id != itemId
                     && item.Path.StartsWith(oldPath + "/", StringComparison.Ordinal)))
        {
            descendant.Path = newPath + descendant.Path[oldPath.Length..];
        }
    }

    public static HashSet<Guid> GetDescendantIds(IReadOnlyList<NotebookItem> items, Guid parentId)
    {
        var ids = new HashSet<Guid>();
        var pending = new Queue<Guid>();
        pending.Enqueue(parentId);

        while (pending.Count > 0)
        {
            var currentId = pending.Dequeue();
            foreach (var child in items.Where(item => item.ParentId == currentId))
            {
                if (ids.Add(child.Id))
                {
                    pending.Enqueue(child.Id);
                }
            }
        }

        return ids;
    }

    public static string GetAuthorDisplayName(IReadOnlyDictionary<Guid, string> displayNames, Guid ownerId)
    {
        return displayNames.TryGetValue(ownerId, out var displayName) ? displayName : "Unknown";
    }

    public static NotebookItemModel ToItemModel(NotebookItem item)
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
            ParseContent(item.ContentJson),
            item.PlainTextContent,
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
