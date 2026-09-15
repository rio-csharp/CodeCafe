using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Models;

namespace CodeCafe.Application.Notebooks.Queries;

public sealed record ListNotebooksQuery(
    string? Tag,
    bool? IsFavorite,
    NotebookVisibility? Visibility,
    string? Cursor,
    int? PageSize)
    : IQuery<Result<CursorPage<NotebookSummaryDto>>>;
