using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Revisions.Models;

namespace CodeCafe.Application.Revisions.Queries;

public sealed record ListBlockRevisionsQuery(Guid PageId, Guid BlockId, string? Cursor, int? PageSize)
    : IQuery<Result<CursorPage<BlockRevisionDto>>>;
