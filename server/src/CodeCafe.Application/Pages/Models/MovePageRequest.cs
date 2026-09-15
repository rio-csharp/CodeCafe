namespace CodeCafe.Application.Pages.Models;

public sealed record MovePageRequest(string? ParentPath, Guid? AfterPageId);
