using CodeCafe.Application.Notebooks.ShareNotebook;
using CodeCafe.Domain.Notebooks;
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
    DateTimeOffset UpdatedAtUtc,
    // Who the caller is to this notebook; both false for anonymous readers.
    bool IsOwner,
    bool CanWrite);
