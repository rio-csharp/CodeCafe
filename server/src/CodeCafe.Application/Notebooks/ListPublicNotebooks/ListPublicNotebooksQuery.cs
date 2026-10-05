using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Abstractions;
using CodeCafe.Application.Notebooks.ListNotebooks;

namespace CodeCafe.Application.Notebooks.ListPublicNotebooks;

public sealed record ListPublicNotebooksQuery(
    string? Search,
    NotebookSort? Sort,
    int? Page,
    int? PageSize)
    : IQuery<Result<PagedResult<NotebookSummaryDto>>>;
