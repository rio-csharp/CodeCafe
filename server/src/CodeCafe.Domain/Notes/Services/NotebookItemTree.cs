using CodeCafe.Domain.Notes.Enums;
using CodeCafe.Domain.Notes.ValueObjects;
namespace CodeCafe.Domain.Notes.Services;

internal static class NotebookItemTree
{
    public static NotebookItemParentViolation? ValidateParentCandidate(
        NotebookItem? parent,
        Guid? parentId
    )
    {
        if (parentId is null)
        {
            return null;
        }

        if (parent is null)
        {
            return NotebookItemParentViolation.NotFound;
        }

        return parent.Type == NotebookItemType.Folder
            ? null
            : NotebookItemParentViolation.NotFolder;
    }

    public static bool WouldCreateCycle(
        IReadOnlyList<NotebookItem> notebookItems,
        Guid itemId,
        Guid proposedParentId,
        IReadOnlyList<(Guid ItemId, Guid? ParentId)>? pendingParentOverrides = null
    )
    {
        var parentMap = notebookItems.ToDictionary(item => item.Id, item => item.ParentId);
        if (pendingParentOverrides is not null)
        {
            foreach (var (overrideItemId, overrideParentId) in pendingParentOverrides)
            {
                parentMap[overrideItemId] = overrideParentId;
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
        Guid currentItemId
    )
    {
        var slugBudget = NotebookPath.GetSlugBudget(parentPath);
        if (slugBudget <= 0)
        {
            throw new ArgumentException(
                "Parent path leaves no room for a child slug; check NotebookPath.HasRoomForChild first.",
                nameof(parentPath)
            );
        }

        var baseSlug = NotebookSlugGenerator.FromTitle(title, "page", slugBudget);
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var slug = NotebookSlugGenerator.WithSuffix(baseSlug, attempt, slugBudget);
            var path = string.IsNullOrWhiteSpace(parentPath) ? slug : $"{parentPath}/{slug}";
            var exists = notebookItems.Any(item => item.Id != currentItemId && item.Path.Value == path);
            if (!exists)
            {
                return path;
            }
        }

        var finalSlug = NotebookSlugGenerator.WithUniqueSuffix(baseSlug, slugBudget);
        return string.IsNullOrWhiteSpace(parentPath) ? finalSlug : $"{parentPath}/{finalSlug}";
    }

    public static void ApplyDescendantPathUpdate(
        IReadOnlyList<NotebookItem> notebookItems,
        Guid itemId,
        string oldPath,
        string newPath
    )
    {
        foreach (
            var descendant in notebookItems.Where(item =>
                item.Id != itemId && item.Path.Value.StartsWith(oldPath + "/", StringComparison.Ordinal)
            )
        )
        {
            descendant.UpdatePath(newPath + descendant.Path.Value[oldPath.Length..]);
        }
    }

    public static bool DescendantsFitAfterMove(
        IReadOnlyList<NotebookItem> notebookItems,
        Guid itemId,
        string oldPath,
        string newPath
    )
    {
        var growth = newPath.Length - oldPath.Length;
        if (growth <= 0)
        {
            return true;
        }

        var prefix = oldPath + "/";
        foreach (var descendant in notebookItems)
        {
            if (
                descendant.Id == itemId
                || !descendant.Path.Value.StartsWith(prefix, StringComparison.Ordinal)
            )
            {
                continue;
            }

            if (descendant.Path.Value.Length + growth > NotebookPath.MaxLength)
            {
                return false;
            }
        }

        return true;
    }

    public static NotebookItemRestoreViolation? ValidateRestore(
        IReadOnlyList<NotebookItem> notebookItems,
        NotebookItem item
    )
    {
        if (!item.IsArchived)
        {
            return NotebookItemRestoreViolation.NotArchived;
        }

        if (item.ParentId is not Guid parentId)
        {
            return null;
        }

        var parent = notebookItems.SingleOrDefault(existingItem => existingItem.Id == parentId);
        if (parent is null)
        {
            return NotebookItemRestoreViolation.ParentNotFound;
        }

        if (parent.IsArchived)
        {
            var subtreeIds = GetDescendantIds(notebookItems, item.Id);
            subtreeIds.Add(item.Id);
            if (!subtreeIds.Contains(parent.Id))
            {
                return NotebookItemRestoreViolation.ParentArchived;
            }
        }

        return null;
    }

    public static HashSet<Guid> GetDescendantIds(IReadOnlyList<NotebookItem> items, Guid parentId)
    {
        var childrenByParent = new Dictionary<Guid, List<Guid>>();
        foreach (var item in items)
        {
            if (item.ParentId is not Guid itemParentId)
            {
                continue;
            }

            if (!childrenByParent.TryGetValue(itemParentId, out var siblings))
            {
                siblings = [];
                childrenByParent[itemParentId] = siblings;
            }

            siblings.Add(item.Id);
        }

        var ids = new HashSet<Guid>();
        var pending = new Queue<Guid>();
        pending.Enqueue(parentId);

        while (pending.Count > 0)
        {
            var currentId = pending.Dequeue();
            if (!childrenByParent.TryGetValue(currentId, out var children))
            {
                continue;
            }

            foreach (var childId in children)
            {
                if (ids.Add(childId))
                {
                    pending.Enqueue(childId);
                }
            }
        }

        return ids;
    }
}
