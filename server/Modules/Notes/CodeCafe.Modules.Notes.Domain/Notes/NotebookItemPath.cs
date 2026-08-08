namespace CodeCafe.Modules.Notes.Domain.Notes;

/// <summary>
/// Path length budgeting for notebook item paths. Paths are materialised strings
/// ("folder/sub/page"), so deep nesting plus long titles can exceed the Path column; these helpers
/// let callers reject such requests as validation failures instead of failing at the database.
/// </summary>
public static class NotebookItemPath
{
    /// <summary>
    /// Upper bound for a materialised path, matching the Path column length.
    /// </summary>
    public const int MaxPathLength = 1024;

    /// <summary>
    /// Slug budget available for a direct child of <paramref name="parentPath"/>, capped by both the
    /// slug column and the remaining path space. Returns 0 when the parent leaves no usable room.
    /// </summary>
    public static int GetSlugBudget(string? parentPath)
    {
        if (string.IsNullOrWhiteSpace(parentPath))
        {
            return Math.Min(NotebookSlugGenerator.MaxSlugLength, MaxPathLength);
        }

        // "parentPath" + "/" + slug
        var remaining = MaxPathLength - parentPath.Length - 1;
        return remaining < NotebookSlugGenerator.MinSlugLength
            ? 0
            : Math.Min(NotebookSlugGenerator.MaxSlugLength, remaining);
    }

    /// <summary>
    /// Whether <paramref name="parentPath"/> can still hold a child with a usable slug.
    /// </summary>
    public static bool HasRoomForChild(string? parentPath)
    {
        return GetSlugBudget(parentPath) > 0;
    }

    /// <summary>
    /// Whether re-rooting a subtree from <paramref name="oldPath"/> to <paramref name="newPath"/>
    /// keeps every descendant path inside the column budget. Only growth matters, so the check is
    /// driven by the longest descendant path in the subtree.
    /// </summary>
    public static bool DescendantsFitAfterMove(
        IReadOnlyList<NotebookItem> notebookItems,
        Guid itemId,
        string oldPath,
        string newPath)
    {
        var growth = newPath.Length - oldPath.Length;
        if (growth <= 0)
        {
            return true;
        }

        var prefix = oldPath + "/";
        foreach (var descendant in notebookItems)
        {
            if (descendant.Id == itemId || !descendant.Path.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (descendant.Path.Length + growth > MaxPathLength)
            {
                return false;
            }
        }

        return true;
    }
}
