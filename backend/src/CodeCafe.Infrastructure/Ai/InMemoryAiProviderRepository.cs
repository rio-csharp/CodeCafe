namespace CodeCafe.Infrastructure.Ai;

using CodeCafe.Application.Ai;
using CodeCafe.Domain.Ai;

public sealed class InMemoryAiProviderRepository : IAiProviderRepository
{
    private readonly List<AiProviderConfiguration> providers =
    [
        BuiltIn("OpenAI", "https://api.openai.com/v1"),
        BuiltIn("Anthropic", "https://api.anthropic.com"),
        BuiltIn("OpenRouter", "https://openrouter.ai/api/v1"),
        BuiltIn("DeepSeek", "https://api.deepseek.com"),
        BuiltIn("MiniMax", "https://api.minimaxi.com/anthropic"),
        BuiltIn("Kimi", "https://api.moonshot.cn/v1"),
        BuiltIn("Google Gemini", "https://generativelanguage.googleapis.com/v1beta"),
        BuiltIn("Groq", "https://api.groq.com/openai/v1"),
        BuiltIn("Mistral", "https://api.mistral.ai/v1"),
        BuiltIn("xAI", "https://api.x.ai/v1"),
        BuiltIn("Azure OpenAI", ""),
        BuiltIn("Ollama", "http://localhost:11434/v1"),
        BuiltIn("LM Studio", "http://localhost:1234/v1"),
        BuiltIn("Custom", "")
    ];
    private readonly Lock syncRoot = new();

    public Task<IReadOnlyCollection<AiProviderConfiguration>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (syncRoot)
        {
            return Task.FromResult<IReadOnlyCollection<AiProviderConfiguration>>(providers.ToArray());
        }
    }

    public Task<AiProviderConfiguration?> GetAsync(Guid providerId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (syncRoot)
        {
            return Task.FromResult(providers.SingleOrDefault(provider => provider.Id == providerId));
        }
    }

    public Task AddAsync(AiProviderConfiguration provider, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (syncRoot)
        {
            providers.Add(provider);
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid providerId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (syncRoot)
        {
            var provider = providers.SingleOrDefault(item => item.Id == providerId);

            if (provider is not null)
            {
                providers.Remove(provider);
            }
        }

        return Task.CompletedTask;
    }

    private static AiProviderConfiguration BuiltIn(string name, string baseUrl)
    {
        return new AiProviderConfiguration(
            name,
            baseUrl,
            apiKey: null,
            enabled: false,
            builtIn: true);
    }
}
