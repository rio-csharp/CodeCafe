using System.Text.Json;

using CodeCafe.Domain.Revisions;

namespace CodeCafe.Application.Revisions.ListBlockRevisions;

public sealed record BlockRevisionDto(
    Guid BlockId,
    long BlockVersion,
    BlockChangeKind ChangeKind,
    JsonElement Content,
    RevisionSource Source,
    DateTimeOffset CreatedAtUtc);
