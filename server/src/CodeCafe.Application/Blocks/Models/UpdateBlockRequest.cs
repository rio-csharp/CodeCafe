using System.Text.Json;

namespace CodeCafe.Application.Blocks.Models;

public sealed record UpdateBlockRequest(JsonElement Content, long BaseRevision);
