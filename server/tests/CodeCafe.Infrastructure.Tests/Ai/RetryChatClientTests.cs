using System.ClientModel;
using CodeCafe.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace CodeCafe.Infrastructure.Tests.Ai;

public sealed class RetryChatClientTests
{
    [Fact]
    public async Task GetResponse_Retries429UntilSuccess()
    {
        var inner = new FlakyChatClient(Failure(429, "rate limited"), failures: 3);
        var client = new RetryChatClient(inner, maxRetries: 4, TimeSpan.Zero);

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "q")], cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(4, inner.Attempts);
    }

    [Fact]
    public async Task GetResponse_ExhaustsRetries_ThenThrows()
    {
        var inner = new FlakyChatClient(Failure(429, "rate limited"), failures: 10);
        var client = new RetryChatClient(inner, maxRetries: 2, TimeSpan.Zero);

        await Assert.ThrowsAsync<ClientResultException>(() => client.GetResponseAsync([new ChatMessage(ChatRole.User, "q")], cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(3, inner.Attempts); // 1 initial + 2 retries
    }

    [Fact]
    public async Task GetResponse_DoesNotRetryClientErrors()
    {
        var inner = new FlakyChatClient(Failure(400, "bad request"), failures: 10);
        var client = new RetryChatClient(inner, maxRetries: 4, TimeSpan.Zero);

        await Assert.ThrowsAsync<ClientResultException>(() => client.GetResponseAsync([new ChatMessage(ChatRole.User, "q")], cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(1, inner.Attempts);
    }

    [Fact]
    public async Task Streaming_RetriesBeforeFirstUpdate_ButNotMidStream()
    {
        var inner = new FlakyChatClient(Failure(500, "boom"), failures: 2);
        var client = new RetryChatClient(inner, maxRetries: 1, TimeSpan.Zero);

        var updates = new List<ChatResponseUpdate>();
        await Assert.ThrowsAsync<ClientResultException>(async () =>
        {
            await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "q")], cancellationToken: TestContext.Current.CancellationToken))
            {
                updates.Add(update);
            }
        });
        // First attempt's pre-stream failure consumed the single retry; the mid-stream failure surfaced.
        Assert.Single(updates);
        Assert.Equal(2, inner.Attempts);
    }

    [Fact]
    public async Task Streaming_RetriesWhenTheProviderReportsATransientErrorUpdate()
    {
        var inner = new ErrorUpdateChatClient("server_is_overloaded", successesAfter: 2);
        var client = new RetryChatClient(inner, maxRetries: 4, TimeSpan.Zero);

        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "q")], cancellationToken: TestContext.Current.CancellationToken))
        {
            updates.Add(update);
        }

        Assert.Equal(3, inner.Attempts);
        Assert.Equal("recovered", Assert.Single(updates.SelectMany(update => update.Contents).OfType<TextContent>()).Text);
        Assert.DoesNotContain(updates.SelectMany(update => update.Contents), content => content is ErrorContent);
    }

    [Fact]
    public async Task Streaming_SurfacesANonTransientErrorUpdateWithoutRetrying()
    {
        var inner = new ErrorUpdateChatClient("access_denied");
        var client = new RetryChatClient(inner, maxRetries: 4, TimeSpan.Zero);

        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "q")], cancellationToken: TestContext.Current.CancellationToken))
        {
            updates.Add(update);
        }

        Assert.Equal(1, inner.Attempts);
        var error = Assert.Single(updates.SelectMany(update => update.Contents).OfType<ErrorContent>());
        Assert.Equal("access_denied", error.ErrorCode);
    }

    // Plays the shape the OpenAI Responses stream really has when an upstream fails: a couple of
    // contentless updates, an ErrorContent, then a NORMAL end of stream — nothing throws. The
    // relay answers an overloaded upstream exactly this way.
    private sealed class ErrorUpdateChatClient(string errorCode, int successesAfter = int.MaxValue) : IChatClient
    {
        public int Attempts { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException("The assistant only streams.");

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            Attempts++;
            yield return new ChatResponseUpdate();
            yield return new ChatResponseUpdate();
            if (Attempts <= successesAfter)
            {
                yield return new ChatResponseUpdate
                {
                    Contents = { new ErrorContent("provider failed") { ErrorCode = errorCode } },
                };
                yield return new ChatResponseUpdate();
                yield break;
            }

            yield return new ChatResponseUpdate(ChatRole.Assistant, "recovered");
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }

    // Status has no public setter; reflection fills it for the fake failure.
    private static ClientResultException Failure(int status, string message)
    {
        var exception = new ClientResultException(message, null);
        typeof(ClientResultException).GetProperty(nameof(ClientResultException.Status))!.SetValue(exception, status);
        return exception;
    }

    // Fails the first `failures` streaming enumerations, then yields one update; the update is
    // followed by one more failure when failures remain odd, simulating a mid-stream break.
    private sealed class FlakyChatClient(Exception exception, int failures) : IChatClient
    {
        private int _failures = failures;

        public int Attempts { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default
        )
        {
            Attempts++;
            return _failures-- > 0 ? throw exception : Task.FromResult(new ChatResponse());
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            Attempts++;
            if (_failures-- > 0)
            {
                if (_failures % 2 == 1)
                {
                    throw exception; // pre-stream failure
                }

                yield return new ChatResponseUpdate(ChatRole.Assistant, "partial");
                throw exception; // mid-stream failure
            }

            yield return new ChatResponseUpdate(ChatRole.Assistant, "ok");
            await Task.CompletedTask;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}
