using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Revisions.ListBlockRevisions;
namespace CodeCafe.Application.Revisions.ListBlockRevisions;

public sealed class ListBlockRevisionsQueryHandler : IQueryHandler<ListBlockRevisionsQuery, Result<CursorPage<BlockRevisionDto>>>
{
    public Task<Result<CursorPage<BlockRevisionDto>>> Handle(ListBlockRevisionsQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
