using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Revisions.Models;
using CodeCafe.Application.Revisions.Queries;
namespace CodeCafe.Application.Revisions.Handlers;

public sealed class ListPageRevisionsQueryHandler : IQueryHandler<ListPageRevisionsQuery, Result<CursorPage<PageRevisionGroupDto>>>
{
    public Task<Result<CursorPage<PageRevisionGroupDto>>> Handle(ListPageRevisionsQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
