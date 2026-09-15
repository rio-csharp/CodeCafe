using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Models;
using CodeCafe.Application.Search.Queries;

namespace CodeCafe.Application.Search.Handlers;

public sealed class SearchAllPagesQueryHandler : IQueryHandler<SearchAllPagesQuery, Result<CursorPage<PageSearchHitDto>>>
{
    public Task<Result<CursorPage<PageSearchHitDto>>> Handle(SearchAllPagesQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
