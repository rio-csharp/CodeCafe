namespace CodeCafe.Application.Ai.StartAiChat;

public sealed record AiChatRequest(IReadOnlyList<AiChatMessage> Messages);

public sealed record AiChatMessage(AiChatRole Role, string Content);

// Inbound roles are caller-authored, so `system` is deliberately absent: accepting it would let
// a caller replace the system message that StartAiChatCommandHandler.BuildMessages assembles.
// That message is server-owned -- see AssistantSystemPrompt.
public enum AiChatRole
{
    User,
    Assistant,
}
