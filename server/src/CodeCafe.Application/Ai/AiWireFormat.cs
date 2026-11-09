namespace CodeCafe.Application.Ai;

// The three wire formats relays typically speak; the factory maps each to an IChatClient.
//
// BaseUrl is per-format, not per-provider. DeepSeek, for example, serves the OpenAI formats at
// https://api.deepseek.com/v1 but its Anthropic-compatible API at
// https://api.deepseek.com/anthropic. Switching WireFormat without switching BaseUrl sends every
// request to the wrong path, so the two move together.
public enum AiWireFormat
{
    OpenAiChatCompletions,
    OpenAiResponses,
    AnthropicMessages,
}
