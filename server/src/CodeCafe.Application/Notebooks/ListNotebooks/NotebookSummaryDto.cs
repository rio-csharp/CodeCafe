using CodeCafe.Application.Notebooks.Shared;
namespace CodeCafe.Application.Notebooks.ListNotebooks;

public sealed record NotebookSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    NotebookVisibility Visibility,
    bool IsFavorite,
    IReadOnlyList<string> Tags,
    int PageCount,
    DateTimeOffset UpdatedAtUtc);
