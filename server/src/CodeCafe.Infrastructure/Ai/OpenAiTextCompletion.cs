using CodeCafe.Application.Ai.Drafts;
using CodeCafe.Application.Ai.Edits;
using CodeCafe.Application.Ai;
using CodeCafe.Application.Notes;
using OpenAI.Chat;
using OpenAI.Responses;
using OpenAI;
using Polly;
using Polly.Retry;

namespace CodeCafe.Infrastructure.Ai;

internal static class OpenAiTextCompletion
{
    private static readonly ResiliencePipeline<string> RetryPipeline = new ResiliencePipelineBuilder<string>()
        .AddRetry(new RetryStrategyOptions<string>
        {
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder<string>()
                .HandleInner<HttpRequestException>()
                .HandleInner<System.ClientModel.ClientResultException>(ex =>
                {
                    // Retry only on transient HTTP errors (5xx, network failure), not on client errors (4xx)
                    if (ex.InnerException is HttpRequestException httpEx)
                    {
                        return httpEx.StatusCode is null or >= System.Net.HttpStatusCode.InternalServerError;
                    }
                    // Retry on network failures wrapped in ClientResultException
                    return ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase)
                        || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);
                })
        })
        .Build();

    /// <summary>
    /// Single boundary where provider SDK failures become <see cref="AiProviderException"/>. Both
    /// generators route through here, so use cases never see an OpenAI or HTTP exception type.
    /// Retries transient failures (5xx, network errors) up to 2 times with exponential backoff.
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
            return await RetryPipeline.ExecuteAsync(async ct =>
            {
                return options.WireFormat == AiWireFormat.Responses
                    ? await CompleteWithResponsesApiAsync(openAiClient, options, instructions, userPrompt, endUserId, ct)
                    : await CompleteWithChatCompletionsAsync(openAiClient, options, instructions, userPrompt, endUserId, ct);
            }, cancellationToken);
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
