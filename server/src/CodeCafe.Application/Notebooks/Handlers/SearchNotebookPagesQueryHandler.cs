using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.Models;
using CodeCafe.Application.Notebooks.Queries;
namespace CodeCafe.Application.Notebooks.Handlers;

public sealed class SearchNotebookPagesQueryHandler : IQueryHandler<SearchNotebookPagesQuery, Result<CursorPage<PageSearchHitDto>>>
{
    public Task<Result<CursorPage<PageSearchHitDto>>> Handle(SearchNotebookPagesQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
