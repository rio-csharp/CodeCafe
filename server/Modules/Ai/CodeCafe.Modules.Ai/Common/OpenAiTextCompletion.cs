using CodeCafe.Modules.Ai.Configuration;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;

namespace CodeCafe.Modules.Ai.Common;

internal static class OpenAiTextCompletion
{
    public static Task<string> CompleteAsync(
        OpenAIClient openAiClient,
        AiOptions options,
        string instructions,
        string userPrompt,
        string endUserId,
        CancellationToken cancellationToken)
    {
        return options.WireFormat == AiWireFormat.Responses
            ? CompleteWithResponsesApiAsync(openAiClient, options, instructions, userPrompt, endUserId, cancellationToken)
            : CompleteWithChatCompletionsAsync(openAiClient, options, instructions, userPrompt, endUserId, cancellationToken);
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
