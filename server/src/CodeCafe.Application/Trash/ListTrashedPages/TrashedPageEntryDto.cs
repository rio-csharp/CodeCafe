namespace CodeCafe.Application.Trash.ListTrashedPages;

public sealed record TrashedPageEntryDto(
    Guid PageId,
    string Title,
    string Slug,
    int DescendantCount,
    DateTimeOffset DeletedAtUtc);
