using Microsoft.Extensions.AI;

namespace CodeCafe.Infrastructure.Ai;

// Compatibility adapter for relays built on new-api (the one-api fork). Known quirks bundled
// here:
// - its Responses conversion rejects role=system messages outright (400 "System messages are
//   not allowed"), so the system prompt moves into ChatOptions.Instructions — the Responses
//   API's native field, which new-api passes through fine;
// - its upstream OAuth tokens churn in waves of 401 token_revoked — handled by RetryChatClient,
//   not here.
// Application code always speaks the canonical system message; only this adapter knows the
// quirks exist.
public sealed class NewApiCompatibilityChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var (rewrittenMessages, rewrittenOptions) = Rewrite(messages, options);
        return base.GetResponseAsync(rewrittenMessages, rewrittenOptions, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var (rewrittenMessages, rewrittenOptions) = Rewrite(messages, options);
        return base.GetStreamingResponseAsync(rewrittenMessages, rewrittenOptions, cancellationToken);
    }

    private static (IEnumerable<ChatMessage>, ChatOptions?) Rewrite(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options
    )
    {
        var systemText = string.Join(
            "\n\n",
            messages.Where(message => message.Role == ChatRole.System).Select(message => message.Text)
        );
        if (systemText.Length == 0)
        {
            return (messages, options);
        }

        var remaining = messages.Where(message => message.Role != ChatRole.System).ToList();
        var merged = options?.Clone() ?? new ChatOptions();
        merged.Instructions = merged.Instructions is null
            ? systemText
            : string.Concat(systemText, "\n\n", merged.Instructions);
        return (remaining, merged);
    }
}
