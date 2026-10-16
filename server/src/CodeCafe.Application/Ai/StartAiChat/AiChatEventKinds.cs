namespace CodeCafe.Application.Ai.StartAiChat;

// Values of AiChatEvent.Kind, consumed by the SSE client and the MCP wrapper.
public static class AiChatEventKinds
{
    public const string Text = "text";
    public const string ToolCall = "tool-call";
    public const string ToolResult = "tool-result";
    public const string Error = "error";
    public const string Done = "done";
}
