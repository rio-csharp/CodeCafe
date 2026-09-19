namespace CodeCafe.Application.Notebooks.Shared;

public sealed record PageSearchHitDto(
    Guid PageId,
    Guid NotebookId,
    string NotebookTitle,
    string Title,
    string Path,
    string Snippet);
