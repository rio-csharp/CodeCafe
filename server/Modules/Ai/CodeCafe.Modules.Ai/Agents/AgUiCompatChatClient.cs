using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace CodeCafe.Modules.Ai.Agents;

/// <summary>
/// Normalizes chat updates for AG-UI protocol compatibility:
/// removes reasoning content (the UI does not render it and the AG-UI client
/// validates reasoning events strictly) and makes sure tool-call updates carry
/// a message id, because AG-UI tool-call events require a parent message id.
/// </summary>
internal sealed class AgUiCompatChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(messages, options, cancellationToken);
        foreach (var message in response.Messages)
        {
            RemoveReasoningContent(message.Contents);
        }

        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            var hadContent = update.Contents.Count > 0;
            RemoveReasoningContent(update.Contents);
            EnsureToolCallMessageId(update);
            if (!hadContent || update.Contents.Count > 0 || update.FinishReason is not null)
            {
                yield return update;
            }
        }
    }

    private static void EnsureToolCallMessageId(ChatResponseUpdate update)
    {
        if (!string.IsNullOrEmpty(update.MessageId))
        {
            return;
        }

        var toolCall = update.Contents.OfType<FunctionCallContent>().FirstOrDefault();
        if (toolCall is not null)
        {
            update.MessageId = $"toolcall-{toolCall.CallId}";
        }
    }

    private static void RemoveReasoningContent(IList<AIContent> contents)
    {
        for (var index = contents.Count - 1; index >= 0; index--)
        {
            if (contents[index] is TextReasoningContent)
            {
                contents.RemoveAt(index);
            }
        }
    }
}
