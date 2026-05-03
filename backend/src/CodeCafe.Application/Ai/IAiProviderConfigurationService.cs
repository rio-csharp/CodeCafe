namespace CodeCafe.Application.Ai;

using CodeCafe.Contracts.Ai;

public interface IAiProviderConfigurationService
{
    Task<IReadOnlyCollection<AiProviderResponse>> ListProvidersAsync(CancellationToken cancellationToken);

    Task<AiProviderResponse?> GetProviderAsync(Guid providerId, CancellationToken cancellationToken);

    Task<AiProviderResponse> CreateProviderAsync(UpsertAiProviderRequest request, CancellationToken cancellationToken);

    Task<AiProviderResponse?> UpdateProviderAsync(Guid providerId, UpsertAiProviderRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteProviderAsync(Guid providerId, CancellationToken cancellationToken);

    Task<AiProviderModelResponse?> CreateModelAsync(Guid providerId, UpsertAiProviderModelRequest request, CancellationToken cancellationToken);

    Task<AiProviderModelResponse?> UpdateModelAsync(Guid providerId, Guid modelId, UpsertAiProviderModelRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteModelAsync(Guid providerId, Guid modelId, CancellationToken cancellationToken);
}
