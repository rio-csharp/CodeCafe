using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Revisions.ListPageRevisions;

namespace CodeCafe.Application.Revisions.ListPageRevisions;

public sealed record ListPageRevisionsQuery(Guid PageId, string? Cursor, int? PageSize)
    : IQuery<Result<CursorPage<PageRevisionGroupDto>>>;
