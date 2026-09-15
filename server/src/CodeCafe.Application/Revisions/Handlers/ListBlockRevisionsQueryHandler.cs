using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Revisions.Models;
using CodeCafe.Application.Revisions.Queries;
namespace CodeCafe.Application.Revisions.Handlers;

public sealed class ListBlockRevisionsQueryHandler : IQueryHandler<ListBlockRevisionsQuery, Result<CursorPage<BlockRevisionDto>>>
{
    public Task<Result<CursorPage<BlockRevisionDto>>> Handle(ListBlockRevisionsQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
