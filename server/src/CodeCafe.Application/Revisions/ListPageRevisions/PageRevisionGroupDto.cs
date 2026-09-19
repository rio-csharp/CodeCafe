using CodeCafe.Application.Revisions.Shared;
using CodeCafe.Application.Revisions.ListBlockRevisions;
namespace CodeCafe.Application.Revisions.ListPageRevisions;

public sealed record PageRevisionGroupDto(
    DateTimeOffset AtUtc,
    RevisionSource Source,
    IReadOnlyList<BlockRevisionDto> Changes);
