namespace CodeCafe.Application.Pages.Shared;

public sealed record PageSearchHitDto(
    Guid PageId,
    Guid NotebookId,
    // The slug travels with every hit so callers can link straight into the reader.
    string NotebookSlug,
    string NotebookTitle,
    string Title,
    string Path,
    string Snippet);
