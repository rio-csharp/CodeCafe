namespace CodeCafe.Application.Pages.CreatePage;

public sealed record CreatePageRequest(
    string Title,
    string? ParentPath
);
