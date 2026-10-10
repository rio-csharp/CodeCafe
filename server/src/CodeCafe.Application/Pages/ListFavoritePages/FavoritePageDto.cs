namespace CodeCafe.Application.Pages.ListFavoritePages;

public sealed record FavoritePageDto(
    Guid PageId,
    string Title,
    string Path,
    Guid NotebookId,
    string NotebookTitle,
    string NotebookSlug
);
