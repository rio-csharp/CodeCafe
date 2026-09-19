using System.Text.Json;

using CodeCafe.Application.Revisions.Shared;
namespace CodeCafe.Application.Revisions.ListBlockRevisions;

public sealed record BlockRevisionDto(
    Guid BlockId,
    long Revision,
    BlockChangeKind ChangeKind,
    JsonElement Content,
    RevisionSource Source,
    DateTimeOffset CreatedAtUtc);
