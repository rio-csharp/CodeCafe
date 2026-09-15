using System.Text.Json;

namespace CodeCafe.Application.Revisions.Models;

public sealed record BlockRevisionDto(
    Guid BlockId,
    long Revision,
    BlockChangeKind ChangeKind,
    JsonElement Content,
    RevisionSource Source,
    DateTimeOffset CreatedAtUtc);
