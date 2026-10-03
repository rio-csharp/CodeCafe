using System.ClientModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace CodeCafe.Infrastructure.Ai;

// Retries transient relay failures (429, 5xx) with exponential backoff. Streaming retries cover
// only the window before the first update: once text has flowed downstream, a replay would
// duplicate it, so mid-stream failures surface to the caller.
public sealed class RetryChatClient(IChatClient innerClient, int maxRetries, TimeSpan baseDelay) : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await base.GetResponseAsync(messages, options, cancellationToken);
            }
            catch (Exception exception) when (attempt < maxRetries && IsTransient(exception))
            {
                await Delay(attempt, cancellationToken);
            }
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        // Retry only until the first update arrives; afterwards the stream is un-replayable.
        IAsyncEnumerator<ChatResponseUpdate>? enumerator = null;
        for (var attempt = 0; ; attempt++)
        {
            enumerator = base
                .GetStreamingResponseAsync(messages, options, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);
            try
            {
                if (await enumerator.MoveNextAsync())
                {
                    break;
                }

                await enumerator.DisposeAsync();
                yield break;
            }
            catch (Exception exception) when (attempt < maxRetries && IsTransient(exception))
            {
                await enumerator.DisposeAsync();
                enumerator = null;
                await Delay(attempt, cancellationToken);
            }
        }

        await using (enumerator)
        {
            yield return enumerator.Current;
            while (await enumerator.MoveNextAsync())
            {
                yield return enumerator.Current;
            }
        }
    }

    // 429 and 5xx are textbook-transient; 401 is retried because relays proxy upstream providers
    // whose OAuth tokens churn — a revoked-upstream-token 401 recovers within seconds there,
    // while a genuinely bad key just costs a few extra attempts before the same failure.
    private static bool IsTransient(Exception exception) =>
        exception is ClientResultException { Status: 401 or 429 } or ClientResultException { Status: >= 500 };

    private async Task Delay(int attempt, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
        await Task.Delay(delay, cancellationToken);
    }
}
