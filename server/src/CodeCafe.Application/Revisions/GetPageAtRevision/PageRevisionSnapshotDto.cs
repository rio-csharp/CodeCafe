using System.Text.Json;

namespace CodeCafe.Application.Revisions.GetPageAtRevision;

/// <summary>
/// One block in a reconstructed historical page state. Deliberately mirrors the
/// live BlockDto wire shape so the reader can render a snapshot unchanged.
/// </summary>
public sealed record PageRevisionBlockDto(
    Guid Id,
    Guid? ParentBlockId,
    string Type,
    JsonElement Content,
    string SortKey,
    long Version,
    DateTimeOffset UpdatedAtUtc
);

public sealed record PageRevisionSnapshotDto(DateTimeOffset AtUtc, IReadOnlyList<PageRevisionBlockDto> Blocks);
