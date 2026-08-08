using CodeCafe.Infrastructure.Ai;
using CodeCafe.Application.Ai;
using CodeCafe.Infrastructure.Ai.Agents;
using Microsoft.Extensions.AI;
using Xunit;

namespace CodeCafe.Server.Tests;

public sealed class AgUiCompatChatClientTests
{
    [Fact]
    public async Task GetStreamingResponseAsync_DropsReasoningOnlyUpdates()
    {
        var updates = new[]
        {
            new ChatResponseUpdate { Contents = { new TextReasoningContent("thinking") } },
            new ChatResponseUpdate { Contents = { new TextContent("Hello") } },
            new ChatResponseUpdate { Contents = { new TextReasoningContent("more"), new TextContent(" world") } },
            new ChatResponseUpdate(),
        };

        var client = new AgUiCompatChatClient(new FakeChatClient(updates));
        var collected = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
        {
            collected.Add(update);
        }

        Assert.Equal(3, collected.Count);
        Assert.All(collected, update => Assert.DoesNotContain(update.Contents, content => content is TextReasoningContent));
        Assert.Equal("Hello", collected[0].Text);
        Assert.Equal(" world", collected[1].Text);
        Assert.Empty(collected[2].Contents);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_AssignsMessageIdToToolCallUpdatesWithoutOne()
    {
        var updates = new[]
        {
            new ChatResponseUpdate
            {
                Contents = { new FunctionCallContent("call-1", "list_notebooks") },
            },
            new ChatResponseUpdate
            {
                MessageId = "msg-existing",
                Contents = { new FunctionCallContent("call-2", "search_notes") },
            },
        };

        var client = new AgUiCompatChatClient(new FakeChatClient(updates));
        var collected = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")]))
        {
            collected.Add(update);
        }

        Assert.Equal("toolcall-call-1", collected[0].MessageId);
        Assert.Equal("msg-existing", collected[1].MessageId);
    }

    [Fact]
    public async Task GetResponseAsync_RemovesReasoningContent()
    {
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant,
        [
            new TextReasoningContent("thinking"),
            new TextContent("answer"),
        ]));

        var client = new AgUiCompatChatClient(new FakeChatClient(response));
        var result = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        var contents = Assert.Single(result.Messages).Contents;
        var text = Assert.Single(contents);
        Assert.IsType<TextContent>(text);
    }

    private sealed class FakeChatClient : IChatClient
    {
        private readonly ChatResponse _response;
        private readonly IReadOnlyList<ChatResponseUpdate> _updates;

        public FakeChatClient(ChatResponse response)
        {
            _response = response;
            _updates = [];
        }

        public FakeChatClient(IReadOnlyList<ChatResponseUpdate> updates)
        {
            _response = new ChatResponse();
            _updates = updates;
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_response);

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var update in _updates)
            {
                yield return update;
            }

            await Task.CompletedTask;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
