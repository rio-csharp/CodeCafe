using CodeCafe.Application.Revisions.ListBlockRevisions;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Revisions.ListPageRevisions;

public sealed record PageRevisionGroupDto(
    DateTimeOffset AtUtc,
    RevisionSource Source,
    IReadOnlyList<BlockRevisionDto> Changes);
