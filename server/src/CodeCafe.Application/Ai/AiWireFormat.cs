namespace CodeCafe.Application.Ai;

// The three wire formats relays typically speak; the factory maps each to an IChatClient.
public enum AiWireFormat
{
    OpenAiChatCompletions,
    OpenAiResponses,
    AnthropicMessages,
}
