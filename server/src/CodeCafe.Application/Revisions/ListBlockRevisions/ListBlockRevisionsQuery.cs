using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Revisions.ListBlockRevisions;

public sealed record ListBlockRevisionsQuery(Guid PageId, Guid BlockId, string? Cursor, int? PageSize)
    : IQuery<Result<CursorPage<BlockRevisionDto>>>;
