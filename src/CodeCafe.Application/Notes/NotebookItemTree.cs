using CodeCafe.Domain.Notes;

namespace CodeCafe.Application.Notes;

public static class NotebookItemTree
{
    public static NotebookItem? FindRequestedParent(
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
        var visited = new HashSet<Guid>();
        while (currentParentId is not null)
        {
            if (currentParentId == itemId)
            {
                return true;
            }

            if (!visited.Add(currentParentId.Value))
            {
                // A pre-existing cycle that does not involve itemId; stop walking.
                return true;
            }

            currentParentId = parentMap.GetValueOrDefault(currentParentId.Value);
        }

        return false;
    }

    public static string GeneratePath(
        IReadOnlyList<NotebookItem> notebookItems,
        string? parentPath,
        string title,
        Guid currentItemId)
    {
        var baseSlug = NotebookSlugGenerator.FromTitle(title, "page");
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var slug = NotebookSlugGenerator.WithSuffix(baseSlug, attempt);
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
}
