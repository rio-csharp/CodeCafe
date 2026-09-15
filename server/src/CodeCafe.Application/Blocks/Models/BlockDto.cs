using System.Text.Json;

namespace CodeCafe.Application.Blocks.Models;

public sealed record BlockDto(
    Guid Id,
    string Type,
    JsonElement Content,
    long Revision,
    DateTimeOffset UpdatedAtUtc);
