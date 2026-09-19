using System.Text.Json;

namespace CodeCafe.Application.Blocks.Shared;

public sealed record BlockInput(string? TempId, string Type, JsonElement Content);
