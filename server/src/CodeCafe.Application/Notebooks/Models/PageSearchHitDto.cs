namespace CodeCafe.Application.Notebooks.Models;

public sealed record PageSearchHitDto(
    Guid PageId,
    Guid NotebookId,
    string NotebookTitle,
    string Title,
    string Path,
    string Snippet);
