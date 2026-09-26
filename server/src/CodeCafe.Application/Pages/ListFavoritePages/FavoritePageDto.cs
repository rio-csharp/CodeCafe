namespace CodeCafe.Application.Pages.ListFavoritePages;

public sealed record FavoritePageDto(Guid PageId, string Title, Guid NotebookId, string NotebookTitle);
