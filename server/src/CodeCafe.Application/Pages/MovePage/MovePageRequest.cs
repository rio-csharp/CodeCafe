namespace CodeCafe.Application.Pages.MovePage;

public sealed record MovePageRequest(string? ParentPath, Guid? AfterPageId);
