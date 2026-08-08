using CodeCafe.Application.Ai.Drafts;
using CodeCafe.Application.Ai.Edits;
using CodeCafe.Application.Ai;
using CodeCafe.Application.Notes;
using OpenAI.Chat;
using OpenAI.Responses;
using OpenAI;

namespace CodeCafe.Infrastructure.Ai;

internal static class OpenAiTextCompletion
{
    /// <summary>
    /// Single boundary where provider SDK failures become <see cref="AiProviderException"/>. Both
    /// generators route through here, so use cases never see an OpenAI or HTTP exception type.
    /// </summary>
    public static async Task<string> CompleteAsync(
        OpenAIClient openAiClient,
        AiOptions options,
        string instructions,
        string userPrompt,
        string endUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            return options.WireFormat == AiWireFormat.Responses
                ? await CompleteWithResponsesApiAsync(openAiClient, options, instructions, userPrompt, endUserId, cancellationToken)
                : await CompleteWithChatCompletionsAsync(openAiClient, options, instructions, userPrompt, endUserId, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The SDK surfaces its own network timeout as a cancellation even though the caller did not
            // cancel; reporting that as a timeout is more accurate than letting it escape unhandled.
            throw new AiProviderException(
                AiFailureKind.Timeout,
                "The AI provider did not respond within the configured network timeout.");
        }
        catch (Exception exception) when (
            exception is System.ClientModel.ClientResultException or HttpRequestException)
        {
            throw new AiProviderException(
                AiFailureKind.Upstream,
                "The AI provider returned an error or was unreachable.",
                exception);
        }
    }

    private static async Task<string> CompleteWithChatCompletionsAsync(
        OpenAIClient openAiClient,
        AiOptions options,
        string instructions,
        string userPrompt,
        string endUserId,
        CancellationToken cancellationToken)
    {
        var client = openAiClient.GetChatClient(options.Model);
        var completionResult = await client.CompleteChatAsync(
            [
                new SystemChatMessage(instructions),
                new UserChatMessage(userPrompt)
            ],
            new ChatCompletionOptions
            {
                MaxOutputTokenCount = Math.Max(1, options.MaxDraftOutputTokens),
                EndUserId = endUserId
            },
            cancellationToken);

        return string.Concat(
            completionResult.Value.Content
                .Where(part => !string.IsNullOrEmpty(part.Text))
                .Select(part => part.Text));
    }

    private static async Task<string> CompleteWithResponsesApiAsync(
        OpenAIClient openAiClient,
        AiOptions options,
        string instructions,
        string userPrompt,
        string endUserId,
        CancellationToken cancellationToken)
    {
        var client = openAiClient.GetResponsesClient();
        var responseResult = await client.CreateResponseAsync(
            new CreateResponseOptions
            {
                Model = options.Model,
                Instructions = instructions,
                InputItems = { ResponseItem.CreateUserMessageItem(userPrompt) },
                MaxOutputTokenCount = Math.Max(1, options.MaxDraftOutputTokens),
                EndUserId = endUserId
            },
            cancellationToken);

        return ExtractOutputText(responseResult.Value);
    }

    private static string ExtractOutputText(ResponseResult response)
    {
        return string.Concat(
            response.OutputItems
                .OfType<MessageResponseItem>()
                .SelectMany(item => item.Content)
                .Where(part => !string.IsNullOrEmpty(part.Text))
                .Select(part => part.Text));
    }
}
