namespace CodeCafe.Domain.Notes;

public static class NotebookItemTree
{
    public static NotebookItem? FindRequestedParent(
        IReadOnlyList<NotebookItem> notebookItems,
        Guid itemId,
        Guid? parentId
    )
    {
        if (parentId is null)
        {
            return null;
        }

        return notebookItems.SingleOrDefault(item => item.Id == parentId && item.Id != itemId);
    }

    /// <summary>
    /// Validates a resolved parent candidate for an item. A null <paramref name="parentId"/>
    /// always denotes a valid root-level placement.
    /// </summary>
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
                // A pre-existing cycle that does not involve itemId; stop walking.
                return true;
            }

            currentParentId = parentMap.GetValueOrDefault(currentParentId.Value);
        }

        return false;
    }

    /// <summary>
    /// Generates a sibling-unique path. The slug is budgeted against the remaining
    /// <see cref="NotebookItemPath.MaxPathLength"/> space so neither the slug nor the path can
    /// overflow its column; callers must check <see cref="NotebookItemPath.HasRoomForChild"/> first,
    /// since a parent with no room left is a validation failure rather than a naming problem.
    /// </summary>
    public static string GeneratePath(
        IReadOnlyList<NotebookItem> notebookItems,
        string? parentPath,
        string title,
        Guid currentItemId
    )
    {
        var slugBudget = NotebookItemPath.GetSlugBudget(parentPath);
        if (slugBudget <= 0)
        {
            throw new ArgumentException(
                "Parent path leaves no room for a child slug; check NotebookItemPath.HasRoomForChild first.",
                nameof(parentPath)
            );
        }

        var baseSlug = NotebookSlugGenerator.FromTitle(title, "page", slugBudget);
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var slug = NotebookSlugGenerator.WithSuffix(baseSlug, attempt, slugBudget);
            var path = string.IsNullOrWhiteSpace(parentPath) ? slug : $"{parentPath}/{slug}";
            var exists = notebookItems.Any(item => item.Id != currentItemId && item.Path == path);
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
                item.Id != itemId && item.Path.StartsWith(oldPath + "/", StringComparison.Ordinal)
            )
        )
        {
            descendant.Path = newPath + descendant.Path[oldPath.Length..];
        }
    }

    /// <summary>
    /// Validates whether an archived item can be restored. Restoring a subtree whose parent
    /// folder is archived is only allowed when the parent itself is part of the subtree being
    /// restored.
    /// </summary>
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
