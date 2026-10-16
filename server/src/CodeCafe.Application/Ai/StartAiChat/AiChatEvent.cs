using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeCafe.Application.Ai.StartAiChat;

// Discriminated union over the five wire events (see AiChatEventKinds). The SSE client and
// the MCP wrapper consume `kind` as the discriminator; each derived record carries exactly
// the fields its kind needs, so illegal states are unrepresentable.
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(AiChatTextEvent), AiChatEventKinds.Text)]
[JsonDerivedType(typeof(AiChatToolCallEvent), AiChatEventKinds.ToolCall)]
[JsonDerivedType(typeof(AiChatToolResultEvent), AiChatEventKinds.ToolResult)]
[JsonDerivedType(typeof(AiChatErrorEvent), AiChatEventKinds.Error)]
[JsonDerivedType(typeof(AiChatDoneEvent), AiChatEventKinds.Done)]
public abstract record AiChatEvent
{
    // In-process view of the JSON discriminator (SSE event name, tests). JsonIgnore is
    // repeated on every override because STJ does not reliably inherit it.
    [JsonIgnore]
    public abstract string Kind { get; }
}

public sealed record AiChatTextEvent(string Text) : AiChatEvent
{
    [JsonIgnore]
    public override string Kind => AiChatEventKinds.Text;
}

public sealed record AiChatToolCallEvent(string Tool, string CallId, JsonElement Arguments) : AiChatEvent
{
    [JsonIgnore]
    public override string Kind => AiChatEventKinds.ToolCall;
}

public sealed record AiChatToolResultEvent(string CallId, JsonElement Result) : AiChatEvent
{
    [JsonIgnore]
    public override string Kind => AiChatEventKinds.ToolResult;
}

public sealed record AiChatErrorEvent(string Code, string Message) : AiChatEvent
{
    [JsonIgnore]
    public override string Kind => AiChatEventKinds.Error;
}

public sealed record AiChatDoneEvent : AiChatEvent
{
    [JsonIgnore]
    public override string Kind => AiChatEventKinds.Done;
}
