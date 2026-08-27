using Microsoft.Extensions.AI;

namespace CodeCafe.Infrastructure.Ai.Agents;

/// <summary>
/// Applies the assistant chat budget: caps output tokens and trims conversation history before the
/// request reaches the provider. Without this, a long-running thread grows unbounded — every turn
/// resends the whole history, so cost per turn climbs with the length of the conversation and a
/// single thread can eventually exceed the model's context window.
/// </summary>
internal sealed class AiBudgetChatClient(
    IChatClient innerClient,
    int maxOutputTokens,
    int maxHistoryMessages,
    int maxHistoryChars
) : DelegatingChatClient(innerClient)
{
    private readonly int maxOutputTokens = Math.Max(1, maxOutputTokens);
    private readonly int maxHistoryMessages = Math.Max(1, maxHistoryMessages);
    private readonly int maxHistoryChars = Math.Max(1, maxHistoryChars);

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return base.GetResponseAsync(
            TrimHistory(messages),
            ApplyOutputBudget(options),
            cancellationToken
        );
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return base.GetStreamingResponseAsync(
            TrimHistory(messages),
            ApplyOutputBudget(options),
            cancellationToken
        );
    }

    /// <summary>
    /// Sets MaxOutputTokens when the caller left it unset. An explicit caller value is respected so
    /// a specific flow can ask for less; the options object is cloned rather than mutated because it
    /// may be reused across requests.
    /// </summary>
    private ChatOptions ApplyOutputBudget(ChatOptions? options)
    {
        if (options is null)
        {
            return new ChatOptions { MaxOutputTokens = maxOutputTokens };
        }

        if (options.MaxOutputTokens is not null)
        {
            return options;
        }

        var budgeted = options.Clone();
        budgeted.MaxOutputTokens = maxOutputTokens;
        return budgeted;
    }

    /// <summary>
    /// Keeps all system messages plus the newest conversation messages that fit in the message and
    /// character budgets. Trimming walks backwards from the newest message because recent turns
    /// carry the most relevant context.
    /// </summary>
    internal IReadOnlyList<ChatMessage> TrimHistory(IEnumerable<ChatMessage> messages)
    {
        var all = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
        var systemMessages = new List<ChatMessage>();
        var conversation = new List<ChatMessage>();

        foreach (var message in all)
        {
            if (message.Role == ChatRole.System)
            {
                systemMessages.Add(message);
            }
            else
            {
                conversation.Add(message);
            }
        }

        var kept = new List<ChatMessage>();
        var charCount = 0;
        for (var index = conversation.Count - 1; index >= 0; index--)
        {
            var message = conversation[index];
            var messageChars = message.Text?.Length ?? 0;
            var wouldExceed =
                kept.Count >= maxHistoryMessages
                || (kept.Count > 0 && charCount + messageChars > maxHistoryChars);
            if (wouldExceed)
            {
                break;
            }

            kept.Add(message);
            charCount += messageChars;
        }

        kept.Reverse();

        // A Tool message answers an earlier tool call. If the cut landed between the two, the
        // provider would reject an orphaned result, so drop leading tool messages.
        var firstKeptIndex = 0;
        while (firstKeptIndex < kept.Count && kept[firstKeptIndex].Role == ChatRole.Tool)
        {
            firstKeptIndex++;
        }

        if (firstKeptIndex > 0)
        {
            kept.RemoveRange(0, firstKeptIndex);
        }

        // Never send an empty conversation: keep the latest message even if it alone busts the
        // character budget, since the provider's own limit is the real backstop there.
        if (kept.Count == 0 && conversation.Count > 0)
        {
            kept.Add(conversation[^1]);
        }

        if (systemMessages.Count == 0)
        {
            return kept;
        }

        var result = new List<ChatMessage>(systemMessages.Count + kept.Count);
        result.AddRange(systemMessages);
        result.AddRange(kept);
        return result;
    }
}
