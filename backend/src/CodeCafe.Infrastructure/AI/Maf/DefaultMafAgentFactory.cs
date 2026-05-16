using Microsoft.Agents.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace CodeCafe.Infrastructure.AI.Maf;

internal sealed class DefaultMafAgentFactory(
    IOptions<MiniMaxAgentOptions> options) : IMafAgentFactory
{
    public AIAgent CreateAgent(string profileId)
    {
        var miniMaxOptions = options.Value;

        return ShouldUseLocalAgent(miniMaxOptions)
            ? new LocalMafAgent(profileId)
            : CreateMiniMaxAgent(profileId, miniMaxOptions);
    }

    private static bool ShouldUseLocalAgent(MiniMaxAgentOptions options)
    {
        return string.Equals(options.Provider, "Local", StringComparison.OrdinalIgnoreCase)
            || (!string.Equals(options.Provider, "MiniMax", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(options.ApiKey));
    }

    private static AIAgent CreateMiniMaxAgent(string profileId, MiniMaxAgentOptions options)
    {
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(options.BaseUrl.TrimEnd('/')),
        };
        ChatClient chatClient = new OpenAIClient(
                new ApiKeyCredential(options.ApiKey),
                clientOptions)
            .GetChatClient(options.Model);

        return chatClient.AsAIAgent(
            instructions: "You are CodeCafe AI, an AI-native engineering workspace assistant.",
            name: profileId,
            description: "MiniMax-backed CodeCafe MAF agent.");
    }
}
