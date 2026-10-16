using System.Runtime.CompilerServices;
using CodeCafe.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace CodeCafe.Infrastructure.Tests.Ai;

public sealed class NewApiCompatibilityChatClientTests
{
    [Fact]
    public async Task SystemMessages_MoveIntoInstructions()
    {
        var inner = new RecordingChatClient();
        var client = new NewApiCompatibilityChatClient(inner);
        ChatMessage[] messages =
        [
            new(ChatRole.System, "prompt A"),
            new(ChatRole.User, "q"),
            new(ChatRole.System, "prompt B"),
        ];

        await client.GetResponseAsync(messages, new ChatOptions { Instructions = "existing" }, TestContext.Current.CancellationToken);

        var (receivedMessages, receivedOptions) = inner.Received.Single();
        Assert.Equal([ChatRole.User], receivedMessages.Select(message => message.Role));
        Assert.Equal("prompt A\n\nprompt B\n\nexisting", receivedOptions!.Instructions);
    }

    [Fact]
    public async Task NoSystemMessages_PassesThroughUntouched()
    {
        var inner = new RecordingChatClient();
        var client = new NewApiCompatibilityChatClient(inner);
        ChatMessage[] messages = [new(ChatRole.User, "q")];

        await client.GetResponseAsync(messages, cancellationToken: TestContext.Current.CancellationToken);

        var (receivedMessages, receivedOptions) = inner.Received.Single();
        Assert.Same(messages, receivedMessages);
        Assert.Null(receivedOptions);
    }

    [Fact]
    public async Task Streaming_AppliesTheSameRewrite()
    {
        var inner = new RecordingChatClient();
        var client = new NewApiCompatibilityChatClient(inner);
        ChatMessage[] messages = [new(ChatRole.System, "prompt"), new(ChatRole.User, "q")];

        await foreach (var _ in client.GetStreamingResponseAsync(messages, cancellationToken: TestContext.Current.CancellationToken)) { }

        var (receivedMessages, receivedOptions) = inner.Received.Single();
        Assert.Equal([ChatRole.User], receivedMessages.Select(message => message.Role));
        Assert.Equal("prompt", receivedOptions!.Instructions);
    }

    private sealed class RecordingChatClient : IChatClient
    {
        public List<(IEnumerable<ChatMessage> Messages, ChatOptions? Options)> Received { get; } = [];

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default
        )
        {
            Received.Add((messages, options));
            return Task.FromResult(new ChatResponse());
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            Received.Add((messages, options));
            await Task.CompletedTask;
            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}
