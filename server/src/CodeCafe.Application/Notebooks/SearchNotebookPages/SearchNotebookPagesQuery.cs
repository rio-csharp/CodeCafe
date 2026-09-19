using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Shared;

namespace CodeCafe.Application.Notebooks.SearchNotebookPages;

public sealed record SearchNotebookPagesQuery(
    string NotebookIdOrSlug,
    string Query,
    string? Cursor,
    int? PageSize) : IQuery<Result<CursorPage<PageSearchHitDto>>>;
