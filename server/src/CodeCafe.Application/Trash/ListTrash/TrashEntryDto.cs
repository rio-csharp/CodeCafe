namespace CodeCafe.Application.Trash.ListTrash;

public sealed record TrashEntryDto(
    Guid NotebookId,
    string Title,
    int PageCount,
    DateTimeOffset DeletedAtUtc);
