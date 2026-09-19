namespace CodeCafe.Application.Ai.StartAiChat;

public sealed record AiChatRequest(IReadOnlyList<AiChatMessage> Messages);

public sealed record AiChatMessage(AiChatRole Role, string Content);

public enum AiChatRole
{
    User,
    Assistant,
}
