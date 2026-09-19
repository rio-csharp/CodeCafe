using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Shared;

namespace CodeCafe.Application.Notebooks.ListNotebooks;

public sealed record ListNotebooksQuery(
    string? Tag,
    bool? IsFavorite,
    NotebookVisibility? Visibility,
    string? Cursor,
    int? PageSize)
    : IQuery<Result<CursorPage<NotebookSummaryDto>>>;
