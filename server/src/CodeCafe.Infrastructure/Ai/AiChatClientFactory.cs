using System.ClientModel;
using System.Diagnostics;
using Anthropic;
using Anthropic.Core;
using CodeCafe.Application.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace CodeCafe.Infrastructure.Ai;

// Three wire formats, one IChatClient: relays just need to be compatible with the chosen format.
public sealed class AiChatClientFactory(AiOptions options, ILogger<RetryChatClient> logger) : IAiChatClientFactory
{
    public IChatClient Create()
    {
        IChatClient client = options.WireFormat switch
        {
            AiWireFormat.OpenAiChatCompletions => OpenAi().GetChatClient(options.Model).AsIChatClient(),
#pragma warning disable OPENAI001 // The Responses client is flagged experimental by the OpenAI SDK; accepted — relays widely support the format and the surface we use is narrow.
            AiWireFormat.OpenAiResponses => OpenAi().GetResponsesClient().AsIChatClient(options.Model),
#pragma warning restore OPENAI001
            // Anthropic requires max tokens per request; the default covers calls that don't set one.
            AiWireFormat.AnthropicMessages => new AnthropicClient(AnthropicOptions())
                .AsIChatClient(options.Model, defaultMaxOutputTokens: options.MaxOutputTokens),
            _ => throw new UnreachableException($"Unknown wire format: {options.WireFormat}"),
        };
        if (options.Relay == AiRelayKind.NewApi)
        {
            client = new NewApiCompatibilityChatClient(client);
        }

        // Retry outermost: a rate-limited attempt never reached the compat rewrite's concern.
        return options.MaxProviderRetries > 0
            ? new RetryChatClient(client, options.MaxProviderRetries, TimeSpan.FromMilliseconds(options.ProviderRetryBaseDelayMs), logger)
            : client;
    }

    private OpenAIClient OpenAi()
    {
        var clientOptions = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            clientOptions.Endpoint = new Uri(options.BaseUrl);
        }

        return new OpenAIClient(new ApiKeyCredential(options.ApiKey), clientOptions);
    }

    private ClientOptions AnthropicOptions()
    {
        var clientOptions = new ClientOptions { ApiKey = options.ApiKey };
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            clientOptions.BaseUrl = options.BaseUrl;
        }

        return clientOptions;
    }
}
