using System.Text.Json;
using CodeCafe.Application.Ai.StartAiChat;

namespace CodeCafe.Application.Tests.Ai;

// Locks the SSE/MCP wire contract: `kind` is the discriminator, and each event carries
// exactly its own camelCase fields (no leftover union-blob members like `tool` on text).
public sealed class AiChatEventSerializationTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void TextEvent_SerializesWithKindAndTextOnly()
    {
        var json = Serialize(new AiChatTextEvent("hello"));

        Assert.Equal("text", json.GetProperty("kind").GetString());
        Assert.Equal("hello", json.GetProperty("text").GetString());
        Assert.Equal(2, json.EnumerateObject().Count());
    }

    [Fact]
    public void ToolCallEvent_SerializesToolCallIdAndArguments()
    {
        AiChatEvent aiEvent = new AiChatToolCallEvent(
            "get_page",
            "call-1",
            JsonSerializer.SerializeToElement(new { pageId = "p1" })
        );

        var json = Serialize(aiEvent);

        Assert.Equal("tool-call", json.GetProperty("kind").GetString());
        Assert.Equal("get_page", json.GetProperty("tool").GetString());
        Assert.Equal("call-1", json.GetProperty("callId").GetString());
        Assert.Equal("p1", json.GetProperty("arguments").GetProperty("pageId").GetString());
    }

    [Fact]
    public void ToolResultEvent_SerializesCallIdAndResult()
    {
        AiChatEvent aiEvent = new AiChatToolResultEvent(
            "call-1",
            JsonSerializer.SerializeToElement(new { ok = true })
        );

        var json = Serialize(aiEvent);

        Assert.Equal("tool-result", json.GetProperty("kind").GetString());
        Assert.Equal("call-1", json.GetProperty("callId").GetString());
        Assert.True(json.GetProperty("result").GetProperty("ok").GetBoolean());
        Assert.False(json.TryGetProperty("tool", out _));
    }

    [Fact]
    public void ErrorEvent_SerializesCodeAndMessage()
    {
        var json = Serialize(new AiChatErrorEvent("ai_disabled", "AI is disabled."));

        Assert.Equal("error", json.GetProperty("kind").GetString());
        Assert.Equal("ai_disabled", json.GetProperty("code").GetString());
        Assert.Equal("AI is disabled.", json.GetProperty("message").GetString());
    }

    [Fact]
    public void DoneEvent_SerializesKindOnly()
    {
        var json = Serialize(new AiChatDoneEvent());

        Assert.Equal("done", json.GetProperty("kind").GetString());
        Assert.Single(json.EnumerateObject());
    }

    // Serializing through the base type (as SseResult and the MCP wrapper do) must still
    // resolve the runtime type's discriminator.
    private static JsonElement Serialize(AiChatEvent aiEvent)
    {
        var json = JsonSerializer.Serialize(aiEvent, WebJson);
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
