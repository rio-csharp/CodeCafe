using System.Text.Json;

namespace CodeCafe.Application.Blocks.Models;

public sealed record BlockInput(string? TempId, string Type, JsonElement Content);
