using System.ClientModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace CodeCafe.Infrastructure.Ai;

// Retries transient relay failures with exponential backoff. Streaming retries cover only the
// window before any content has been produced: once a text or tool-call update is ready, a replay
// would duplicate it, so mid-stream failures surface to the caller. The window spans leading
// updates that carry no content (response.created, response.in_progress) — see
// IsTransientFailure for why a provider failure often arrives as one of those windows rather than
// as an exception.
public sealed class RetryChatClient(IChatClient innerClient, int maxRetries, TimeSpan baseDelay, ILogger? logger = null) : DelegatingChatClient(innerClient)
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
                await Delay(attempt, exception.Message, cancellationToken);
            }
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        // Retry only while nothing replayable has been produced. Leading contentless updates are
        // held back instead of yielded, so a failure behind them still leaves the window open.
        List<ChatResponseUpdate> leading = [];
        IAsyncEnumerator<ChatResponseUpdate>? enumerator = null;
        var hasCurrent = false;

        for (var attempt = 0; ; attempt++)
        {
            leading.Clear();
            hasCurrent = false;
            enumerator = base
                .GetStreamingResponseAsync(messages, options, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);
            var retry = false;
            var reason = string.Empty;
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    if (ProducesContent(enumerator.Current))
                    {
                        hasCurrent = true;
                        break;
                    }

                    leading.Add(enumerator.Current);
                    if (attempt < maxRetries && IsTransientFailure(enumerator.Current))
                    {
                        reason = DescribeTransientFailure(enumerator.Current);
                        retry = true;
                        break;
                    }
                }
            }
            catch (Exception exception) when (attempt < maxRetries && IsTransient(exception))
            {
                reason = exception.Message;
                retry = true;
            }

            if (!retry)
            {
                break;
            }

            await enumerator.DisposeAsync();
            enumerator = null;
            await Delay(attempt, reason, cancellationToken);
        }

        await using (enumerator)
        {
            foreach (var update in leading)
            {
                yield return update;
            }

            if (!hasCurrent)
            {
                yield break;
            }

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

    // The OpenAI Responses stream reports a provider-side failure as an ErrorContent update and
    // then ends the stream NORMALLY, so nothing throws and the exception filter above never runs.
    // Only the codes that mean "the upstream was busy" are retried; a rejection the caller cannot
    // retry away (a denied room, a filtered request) surfaces immediately.
    private static bool IsTransientFailure(ChatResponseUpdate update) =>
        update.Contents.OfType<ErrorContent>().Any(error => TransientErrorCodes.Contains(error.ErrorCode ?? string.Empty));

    private static string DescribeTransientFailure(ChatResponseUpdate update) =>
        update
            .Contents.OfType<ErrorContent>()
            .Select(error => string.IsNullOrWhiteSpace(error.Message) ? error.ErrorCode : error.Message)
            .FirstOrDefault(reason => !string.IsNullOrWhiteSpace(reason))
        ?? "provider reported a transient failure";

    private static readonly HashSet<string> TransientErrorCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "server_is_overloaded",
        "rate_limit_exceeded",
        "server_error",
        "internal_error",
        "timeout",
    };

    // Anything the caller can act on: buffering continues until one of these shows up.
    private static bool ProducesContent(ChatResponseUpdate update) =>
        update.Contents.Any(content => content is TextContent or FunctionCallContent or FunctionResultContent);

    private async Task Delay(int attempt, string reason, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
        logger?.LogWarning(
            "Transient AI provider failure ({Reason}); retrying in {DelayMs} ms (attempt {Attempt} of {MaxRetries}).",
            reason,
            (int)delay.TotalMilliseconds,
            attempt + 1,
            maxRetries
        );
        await Task.Delay(delay, cancellationToken);
    }
}
