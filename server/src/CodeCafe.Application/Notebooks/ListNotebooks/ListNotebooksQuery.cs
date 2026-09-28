using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Domain.Notebooks;

namespace CodeCafe.Application.Notebooks.ListNotebooks;

public sealed record ListNotebooksQuery(
    string? Tag,
    bool? IsFavorite,
    NotebookVisibility? Visibility,
    string? Search,
    NotebookSort? Sort,
    int? Page,
    int? PageSize)
    : IQuery<Result<PagedResult<NotebookSummaryDto>>>;
