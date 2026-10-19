using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Revisions.GetPageAtRevision;

public sealed record GetPageAtRevisionQuery(Guid PageId, DateTimeOffset AtUtc, string? AccessCode = null)
    : IQuery<Result<PageRevisionSnapshotDto>>;
