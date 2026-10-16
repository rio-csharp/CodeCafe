using CodeCafe.Domain.Notebooks;
namespace CodeCafe.Application.Notebooks.ListNotebooks;

public sealed record NotebookSummaryDto(
    Guid Id,
    string Title,
    string? Description,
    string Slug,
    NotebookVisibility Visibility,
    bool IsFavorite,
    IReadOnlyList<string> Tags,
    int PageCount,
    DateTimeOffset UpdatedAtUtc,
    // Attribution matters on a sharing product: every card shows whose book this is.
    string OwnerDisplayName);
