using CodeCafe.Application.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Infrastructure.AI.Maf;

internal static class MafInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddMafInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<MiniMaxAgentOptions>()
            .Bind(configuration.GetSection(MiniMaxAgentOptions.SectionName))
            .PostConfigure(options =>
            {
                options.ApiKey = FirstConfiguredValue(
                    options.ApiKey,
                    Environment.GetEnvironmentVariable("MINIMAX_API_KEY"));
                options.BaseUrl = FirstConfiguredValue(
                    options.BaseUrl,
                    Environment.GetEnvironmentVariable("MINIMAX_BASE_URL"),
                    MiniMaxAgentOptions.DefaultBaseUrl);
                options.Model = FirstConfiguredValue(
                    options.Model,
                    Environment.GetEnvironmentVariable("MINIMAX_MODEL"),
                    MiniMaxAgentOptions.DefaultModel);
            });

        services.AddSingleton<IMafAgentFactory, DefaultMafAgentFactory>();
        services.AddSingleton<IAgentRuntime, MafAgentRuntime>();

        return services;
    }

    private static string FirstConfiguredValue(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }
}
