using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.SearchAllPages;

public sealed record SearchAllPagesQuery(string Query, string? Cursor, int? PageSize)
    : IQuery<Result<CursorPage<PageSearchHitDto>>>;
