using System.Text.Json;

namespace CodeCafe.Application.Ai.Models;

public sealed record AiChatEvent(
    string Kind,
    string? Tool,
    string? Message,
    JsonElement? Payload);
