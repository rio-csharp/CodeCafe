using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.SearchAllPages;

public sealed class SearchAllPagesQueryHandler : IQueryHandler<SearchAllPagesQuery, Result<CursorPage<PageSearchHitDto>>>
{
    public Task<Result<CursorPage<PageSearchHitDto>>> Handle(SearchAllPagesQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
