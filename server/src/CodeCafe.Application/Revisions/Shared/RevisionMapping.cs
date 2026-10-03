using System.Text.Json;

using CodeCafe.Application.Revisions.ListBlockRevisions;
using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Revisions.Shared;

public static class RevisionMapping
{
    // Same live-document trick as BlockMapping.ToDto: hand the response serializer a JsonElement
    // instead of a raw string that would double-encode.
    public static BlockRevisionDto ToDto(BlockRevision revision)
        => new(
            revision.BlockId,
            revision.BlockVersion,
            revision.ChangeKind,
            JsonSerializer.Deserialize<JsonElement>(revision.ContentJson),
            revision.Source,
            revision.CreatedAtUtc
        );
}
