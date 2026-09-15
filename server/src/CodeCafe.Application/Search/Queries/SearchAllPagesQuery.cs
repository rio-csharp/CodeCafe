using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Models;

namespace CodeCafe.Application.Search.Queries;

public sealed record SearchAllPagesQuery(string Query, string? Cursor, int? PageSize)
    : IQuery<Result<CursorPage<PageSearchHitDto>>>;
