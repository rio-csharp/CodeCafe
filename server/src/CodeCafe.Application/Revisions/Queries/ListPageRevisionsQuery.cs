using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Revisions.Models;

namespace CodeCafe.Application.Revisions.Queries;

public sealed record ListPageRevisionsQuery(Guid PageId, string? Cursor, int? PageSize)
    : IQuery<Result<CursorPage<PageRevisionGroupDto>>>;
