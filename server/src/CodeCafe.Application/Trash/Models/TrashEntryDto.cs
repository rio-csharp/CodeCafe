namespace CodeCafe.Application.Trash.Models;

public sealed record TrashEntryDto(
    Guid NotebookId,
    string Title,
    int PageCount,
    DateTimeOffset DeletedAtUtc);
