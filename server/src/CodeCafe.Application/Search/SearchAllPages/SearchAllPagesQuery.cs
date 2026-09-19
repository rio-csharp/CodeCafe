using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Shared;

namespace CodeCafe.Application.Search.SearchAllPages;

public sealed record SearchAllPagesQuery(string Query, string? Cursor, int? PageSize)
    : IQuery<Result<CursorPage<PageSearchHitDto>>>;
