using System.Text.Json;

namespace CodeCafe.Application.Blocks.ApplyBlockOps;

// One result per op, in op order. Content carries the canonical normalized payload for
// insert/update ops (and for everything applicable under DryRun), so agents see exactly what
// would be stored.
public sealed record BlockOpResultDto(string? TempId, Guid? BlockId, long? Version, JsonElement? Content = null);
