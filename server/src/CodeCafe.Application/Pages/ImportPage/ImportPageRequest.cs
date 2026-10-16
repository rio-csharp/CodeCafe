namespace CodeCafe.Application.Pages.ImportPage;

public sealed record ImportPageRequest(string FileName, string Markdown, string? ParentPath = null);
