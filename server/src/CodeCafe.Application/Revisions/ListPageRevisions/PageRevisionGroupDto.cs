using CodeCafe.Application.Revisions.ListBlockRevisions;
using CodeCafe.Application.Revisions.Shared;
namespace CodeCafe.Application.Revisions.ListPageRevisions;

public sealed record PageRevisionGroupDto(
    DateTimeOffset AtUtc,
    RevisionSource Source,
    IReadOnlyList<BlockRevisionDto> Changes);
