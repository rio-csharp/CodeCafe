using CodeCafe.Application.Notes;

namespace CodeCafe.Modules.Ai.Edits;

/// <summary>
/// Notebook-context tree resolution shared by the edit-proposal handlers.
/// The snapshot-based parent/sort-order resolution is kept as-is from the
/// original endpoints: item creation itself goes through the Notes module's
/// <c>CreateNotebookItemCommand</c>, which requires an explicit sort order and
/// validates the parent again server-side.
/// </summary>
internal static class NotebookContextTree
{
    public static NotesResult<NotebookContextItemModel?> ResolveParentForCreate(
        NotebookContextModel notebook,
        string? parentPath)
    {
        if (!string.IsNullOrWhiteSpace(parentPath))
        {
            var normalizedPath = NotebookInput.NormalizePath(parentPath);
            var parent = notebook.Items.SingleOrDefault(item =>
                string.Equals(item.Path, normalizedPath, StringComparison.Ordinal)
                && string.Equals(item.Type, "folder", StringComparison.OrdinalIgnoreCase));

            return parent is null
                ? NotesResult<NotebookContextItemModel?>.Failure(NotesFailureKind.Validation, "invalid_parent", "Parent folder was not found.")
                : NotesResult<NotebookContextItemModel?>.Success(parent);
        }

        return NotesResult<NotebookContextItemModel?>.Success(null);
    }

    public static int ResolveCreateSortOrder(NotebookContextModel notebook, Guid? parentId)
    {
        var siblings = notebook.Items.Where(item => item.ParentId == parentId).ToList();
        return siblings.Count == 0 ? 0 : siblings.Max(item => item.SortOrder) + 1;
    }

    public static string? ResolveParentPathFromItem(NotebookContextModel notebook, Guid? parentId)
    {
        return parentId is null
            ? null
            : notebook.Items.SingleOrDefault(item => item.Id == parentId)?.Path;
    }
}
