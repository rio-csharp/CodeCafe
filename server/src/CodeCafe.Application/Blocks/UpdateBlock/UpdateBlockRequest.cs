using System.Text.Json;

namespace CodeCafe.Application.Blocks.UpdateBlock;

public sealed record UpdateBlockRequest(JsonElement Content, long BaseVersion);
