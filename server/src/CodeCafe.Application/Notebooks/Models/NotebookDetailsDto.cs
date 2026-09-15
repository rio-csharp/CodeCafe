namespace CodeCafe.Application.Notebooks.Models;

public sealed record NotebookDetailsDto(
    Guid Id,
    string Title,
    string? Description,
    string Slug,
    NotebookVisibility Visibility,
    bool HasAccessCode,
    IReadOnlyList<string> Tags,
    IReadOnlyList<NotebookShareDto> Shares,
    int PageCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
