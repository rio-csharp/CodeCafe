namespace CodeCafe.Application.Notebooks.GetNotebookTree;

public sealed record PageTreeNodeDto(
    Guid Id,
    string Title,
    string Path,
    int SortOrder,
    bool IsArchived,
    bool IsFavorite,
    IReadOnlyList<PageTreeNodeDto> Children);
