using CodeCafe.Infrastructure.Ai.Agents;
using CodeCafe.Application.Ai;
using CodeCafe.Application.Ai.Drafts;
using CodeCafe.Application.Ai.Edits;
using CodeCafe.Infrastructure.Ai;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;

namespace CodeCafe.Host.AgUi;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodeCafeAi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddAGUI();
        services.AddMemoryCache();

        // Ai use-case handlers run through the same MediatR pipeline (logging,
        // validation) that Notes.Application registers for the host.
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(AiAssemblyMarker).Assembly));

        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .Validate(options => !options.Enabled
                || (!string.IsNullOrWhiteSpace(options.EndpointPath)
                    && options.EndpointPath.StartsWith("/", StringComparison.Ordinal)),
                "Ai:EndpointPath must start with '/' when AI is enabled.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.StatusEndpointPath)
                && options.StatusEndpointPath.StartsWith("/", StringComparison.Ordinal),
                "Ai:StatusEndpointPath must start with '/'.")
            .Validate(options => !options.Enabled
                || (!string.IsNullOrWhiteSpace(options.EditEndpointPath)
                    && options.EditEndpointPath.StartsWith("/", StringComparison.Ordinal)),
                "Ai:EditEndpointPath must start with '/' when AI is enabled.")
            .Validate(options => !options.Enabled
                || (!string.IsNullOrWhiteSpace(options.DraftEndpointPath)
                    && options.DraftEndpointPath.StartsWith("/", StringComparison.Ordinal)),
                "Ai:DraftEndpointPath must start with '/' when AI is enabled.")
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.AgentName),
                "Ai:AgentName is required when AI is enabled.")
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.Model),
                "Ai:Model is required when AI is enabled.")
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.ApiKey),
                "Ai:ApiKey is required when AI is enabled.")
            .Validate(options => !options.Enabled
                || string.IsNullOrWhiteSpace(options.BaseUrl)
                || (Uri.TryCreate(options.BaseUrl.Trim(), UriKind.Absolute, out var uri)
                    && uri.Scheme is "http" or "https"),
                "Ai:BaseUrl must be an absolute HTTP or HTTPS URL when set.")
            .Validate(options => options.MaxToolResults > 0,
                "Ai:MaxToolResults must be greater than zero.")
            .Validate(options => options.MaxToolContentChars > 0,
                "Ai:MaxToolContentChars must be greater than zero.")
            .Validate(options => options.MaxDraftPromptChars > 0,
                "Ai:MaxDraftPromptChars must be greater than zero.")
            .Validate(options => options.MaxDraftContextChars > 0,
                "Ai:MaxDraftContextChars must be greater than zero.")
            .Validate(options => options.MaxDraftOutputTokens > 0,
                "Ai:MaxDraftOutputTokens must be greater than zero.")
            .Validate(options => options.MaxChatOutputTokens > 0,
                "Ai:MaxChatOutputTokens must be greater than zero.")
            .Validate(options => options.MaxChatHistoryMessages > 0,
                "Ai:MaxChatHistoryMessages must be greater than zero.")
            .Validate(options => options.MaxChatHistoryChars > 0,
                "Ai:MaxChatHistoryChars must be greater than zero.")
            .Validate(options => options.MaxAgUiContextEntries > 0,
                "Ai:MaxAgUiContextEntries must be greater than zero.")
            .Validate(options => options.NetworkTimeoutSeconds > 0,
                "Ai:NetworkTimeoutSeconds must be greater than zero.")
            .ValidateOnStart();

        services.AddSingleton<NotebookAssistantTools>();
        services.TryAddSingleton(serviceProvider =>
            OpenAiClientFactory.Create(serviceProvider.GetRequiredService<IOptions<AiOptions>>().Value));
        services.TryAddScoped<IAiNotebookEditProposalStore, DatabaseAiNotebookEditProposalStore>();
        services.TryAddScoped<IAiNotebookEditGenerator, OpenAiNotebookEditGenerator>();
        services.TryAddScoped<IAiNoteDraftGenerator, OpenAiNoteDraftGenerator>();
        if (!environment.IsEnvironment("Testing"))
        {
            services.AddHostedService<AiNotebookEditProposalCleanupService>();
        }

        var configuredOptions = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
        services.AddAIAgent(
            AiHelpers.NormalizeAgentName(configuredOptions.AgentName),
            AssistantAgentFactory.Create,
            ServiceLifetime.Singleton);

        return services;
    }

}
