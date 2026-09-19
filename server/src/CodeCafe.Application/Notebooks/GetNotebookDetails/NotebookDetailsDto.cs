using CodeCafe.Application.Notebooks.Shared;
using CodeCafe.Application.Notebooks.ShareNotebook;
namespace CodeCafe.Application.Notebooks.GetNotebookDetails;

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
