using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Shared;
namespace CodeCafe.Application.Notebooks.SearchNotebookPages;

public sealed class SearchNotebookPagesQueryHandler : IQueryHandler<SearchNotebookPagesQuery, Result<CursorPage<PageSearchHitDto>>>
{
    public Task<Result<CursorPage<PageSearchHitDto>>> Handle(SearchNotebookPagesQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
