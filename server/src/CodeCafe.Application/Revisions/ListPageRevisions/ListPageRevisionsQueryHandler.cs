using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Revisions.ListPageRevisions;

public sealed class ListPageRevisionsQueryHandler : IQueryHandler<ListPageRevisionsQuery, Result<CursorPage<PageRevisionGroupDto>>>
{
    public Task<Result<CursorPage<PageRevisionGroupDto>>> Handle(ListPageRevisionsQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
