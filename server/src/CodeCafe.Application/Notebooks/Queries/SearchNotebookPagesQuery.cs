using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Models;

namespace CodeCafe.Application.Notebooks.Queries;

public sealed record SearchNotebookPagesQuery(
    string NotebookIdOrSlug,
    string Query,
    string? Cursor,
    int? PageSize) : IQuery<Result<CursorPage<PageSearchHitDto>>>;
