using System.Text.Json;

namespace CodeCafe.Application.Blocks.Shared;

// Flat shape; clients assemble the tree from ParentBlockId and order siblings by SortKey.
public sealed record BlockDto(
    Guid Id,
    Guid? ParentBlockId,
    string Type,
    JsonElement Content,
    string SortKey,
    long Version,
    DateTimeOffset UpdatedAtUtc);
