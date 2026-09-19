using System.Text.Json;

namespace CodeCafe.Application.Ai.StartAiChat;

public sealed record AiChatEvent(
    string Kind,
    string? Tool,
    string? Message,
    JsonElement? Payload);
