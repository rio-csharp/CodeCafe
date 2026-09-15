namespace CodeCafe.Application.Revisions.Models;

public sealed record PageRevisionGroupDto(
    DateTimeOffset AtUtc,
    RevisionSource Source,
    IReadOnlyList<BlockRevisionDto> Changes);
