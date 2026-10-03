using Microsoft.Extensions.AI;

namespace CodeCafe.Application.Tests.Ai;

// Plays back queued batches of updates, one batch per streaming call, so tests can script
// multi-round tool loops. Records every call for prompt-shape assertions.
internal sealed class StubChatClient : IChatClient
{
    private readonly Queue<IReadOnlyList<ChatResponseUpdate>> _batches = new();

    public List<IReadOnlyList<ChatMessage>> ReceivedMessages { get; } = [];

    public List<ChatOptions?> ReceivedOptions { get; } = [];

    public Exception? ExceptionToThrow { get; set; }

    public void Enqueue(params ChatResponseUpdate[] updates) => _batches.Enqueue(updates);

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    ) => throw new NotSupportedException("The assistant only streams.");

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ReceivedMessages.Add(messages.ToList());
        ReceivedOptions.Add(options);
        var batch = _batches.Count > 0 ? _batches.Dequeue() : [];
        return ExceptionToThrow is { } exception ? Throw(exception) : Stream(batch);

        static async IAsyncEnumerable<ChatResponseUpdate> Stream(IReadOnlyList<ChatResponseUpdate> updates)
        {
            foreach (var update in updates)
            {
                yield return update;
            }

            await Task.CompletedTask;
        }

        static async IAsyncEnumerable<ChatResponseUpdate> Throw(Exception exception)
        {
            yield return await Task.FromException<ChatResponseUpdate>(exception);
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
