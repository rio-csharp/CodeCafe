using CodeCafe.Infrastructure.Ai.Agents;
using Microsoft.Extensions.AI;
using Xunit;

namespace CodeCafe.Host.Tests;

/// <summary>
/// Chat budget enforcement. Every turn resends the whole history, so without trimming the cost per
/// turn grows with the conversation and a long thread eventually exceeds the model's context window.
/// </summary>
public sealed class AiBudgetChatClientTests
{
    [Fact]
    public void TrimHistory_KeepsNewestMessagesWithinMessageBudget()
    {
        var client = CreateClient(maxHistoryMessages: 3);
        var messages = Enumerable
            .Range(0, 10)
            .Select(index => new ChatMessage(ChatRole.User, $"m{index}"))
            .ToList();

        var trimmed = client.TrimHistory(messages);

        Assert.Equal(3, trimmed.Count);
        Assert.Equal(["m7", "m8", "m9"], trimmed.Select(message => message.Text));
    }

    [Fact]
    public void TrimHistory_AlwaysKeepsSystemMessages()
    {
        var client = CreateClient(maxHistoryMessages: 2);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "you are an assistant"),
            new(ChatRole.User, "m0"),
            new(ChatRole.Assistant, "m1"),
            new(ChatRole.User, "m2"),
        };

        var trimmed = client.TrimHistory(messages);

        Assert.Equal(ChatRole.System, trimmed[0].Role);
        Assert.Equal("you are an assistant", trimmed[0].Text);
        Assert.Equal(["m1", "m2"], trimmed.Skip(1).Select(message => message.Text));
    }

    [Fact]
    public void TrimHistory_StopsAtCharacterBudget()
    {
        var client = CreateClient(maxHistoryMessages: 100, maxHistoryChars: 25);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, new string('a', 20)),
            new(ChatRole.User, new string('b', 20)),
            new(ChatRole.User, new string('c', 20)),
        };

        var trimmed = client.TrimHistory(messages);

        // Only the newest fits: adding the previous one would reach 40 characters.
        var kept = Assert.Single(trimmed);
        Assert.Equal(new string('c', 20), kept.Text);
    }

    [Fact]
    public void TrimHistory_KeepsTheLatestMessageEvenWhenItAloneExceedsTheCharacterBudget()
    {
        // Sending an empty conversation would be worse than sending one oversized message; the
        // provider's own limit is the real backstop.
        var client = CreateClient(maxHistoryMessages: 100, maxHistoryChars: 10);
        var messages = new List<ChatMessage> { new(ChatRole.User, new string('a', 500)) };

        var trimmed = client.TrimHistory(messages);

        var kept = Assert.Single(trimmed);
        Assert.Equal(500, kept.Text!.Length);
    }

    [Fact]
    public void TrimHistory_DropsLeadingToolMessagesOrphanedByTheCut()
    {
        // A Tool message answers an earlier tool call. If the cut lands between the two, the
        // provider rejects the orphaned result.
        var client = CreateClient(maxHistoryMessages: 2);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "search for x"),
            new(ChatRole.Assistant, "calling tool"),
            new(ChatRole.Tool, "tool result"),
            new(ChatRole.Assistant, "here you go"),
        };

        var trimmed = client.TrimHistory(messages);

        Assert.DoesNotContain(trimmed, message => message.Role == ChatRole.Tool);
        var kept = Assert.Single(trimmed);
        Assert.Equal("here you go", kept.Text);
    }

    [Fact]
    public void TrimHistory_LeavesShortConversationsUntouched()
    {
        var client = CreateClient(maxHistoryMessages: 40, maxHistoryChars: 100000);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "hello"),
            new(ChatRole.Assistant, "hi"),
        };

        var trimmed = client.TrimHistory(messages);

        Assert.Equal(["hello", "hi"], trimmed.Select(message => message.Text));
    }

    private static AiBudgetChatClient CreateClient(
        int maxHistoryMessages,
        int maxHistoryChars = 100000,
        int maxOutputTokens = 1600
    )
    {
        return new AiBudgetChatClient(
            new StubChatClient(),
            maxOutputTokens,
            maxHistoryMessages,
            maxHistoryChars
        );
    }

    private sealed class StubChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default
        ) => AsyncEnumerable.Empty<ChatResponseUpdate>();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}
