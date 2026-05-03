namespace CodeCafe.Application.Ai;

using CodeCafe.Contracts.Ai;
using CodeCafe.Domain.Ai;

public sealed class AiProviderConfigurationService(IAiProviderRepository repository)
    : IAiProviderConfigurationService
{
    public async Task<IReadOnlyCollection<AiProviderResponse>> ListProvidersAsync(CancellationToken cancellationToken)
    {
        var providers = await repository.ListAsync(cancellationToken);

        return providers.Select(MapProvider).ToArray();
    }

    public async Task<AiProviderResponse?> GetProviderAsync(Guid providerId, CancellationToken cancellationToken)
    {
        var provider = await repository.GetAsync(providerId, cancellationToken);

        return provider is null ? null : MapProvider(provider);
    }

    public async Task<AiProviderResponse> CreateProviderAsync(UpsertAiProviderRequest request, CancellationToken cancellationToken)
    {
        var provider = new AiProviderConfiguration(
            request.Name,
            request.BaseUrl,
            request.ApiKey,
            request.Enabled,
            builtIn: false);

        await repository.AddAsync(provider, cancellationToken);

        return MapProvider(provider);
    }

    public async Task<AiProviderResponse?> UpdateProviderAsync(Guid providerId, UpsertAiProviderRequest request, CancellationToken cancellationToken)
    {
        var provider = await repository.GetAsync(providerId, cancellationToken);

        if (provider is null)
        {
            return null;
        }

        provider.Update(request.Name, request.BaseUrl, request.ApiKey, request.Enabled);

        return MapProvider(provider);
    }

    public async Task<bool> DeleteProviderAsync(Guid providerId, CancellationToken cancellationToken)
    {
        var provider = await repository.GetAsync(providerId, cancellationToken);

        if (provider is null)
        {
            return false;
        }

        await repository.DeleteAsync(providerId, cancellationToken);

        return true;
    }

    public async Task<AiProviderModelResponse?> CreateModelAsync(Guid providerId, UpsertAiProviderModelRequest request, CancellationToken cancellationToken)
    {
        var provider = await repository.GetAsync(providerId, cancellationToken);

        if (provider is null)
        {
            return null;
        }

        var model = provider.AddModel(new AiProviderModel(
            request.ModelId,
            request.DisplayName,
            request.Enabled,
            ParseModelKind(request.Kind)));

        return MapModel(model);
    }

    public async Task<AiProviderModelResponse?> UpdateModelAsync(Guid providerId, Guid modelId, UpsertAiProviderModelRequest request, CancellationToken cancellationToken)
    {
        var model = await GetModelAsync(providerId, modelId, cancellationToken);

        if (model is null)
        {
            return null;
        }

        model.Update(
            request.ModelId,
            request.DisplayName,
            request.Enabled,
            ParseModelKind(request.Kind));

        return MapModel(model);
    }

    public async Task<bool> DeleteModelAsync(Guid providerId, Guid modelId, CancellationToken cancellationToken)
    {
        var provider = await repository.GetAsync(providerId, cancellationToken);

        return provider is not null && provider.RemoveModel(modelId);
    }

    private async Task<AiProviderModel?> GetModelAsync(Guid providerId, Guid modelId, CancellationToken cancellationToken)
    {
        var provider = await repository.GetAsync(providerId, cancellationToken);

        return provider?.Models.SingleOrDefault(model => model.Id == modelId);
    }

    private static AiProviderModelKind ParseModelKind(string kind)
    {
        return Enum.TryParse<AiProviderModelKind>(kind, ignoreCase: true, out var parsed)
            ? parsed
            : AiProviderModelKind.Custom;
    }

    private static AiProviderResponse MapProvider(AiProviderConfiguration provider)
    {
        return new AiProviderResponse(
            provider.Id,
            provider.Name,
            provider.BaseUrl,
            provider.ApiKey,
            provider.Enabled,
            provider.BuiltIn,
            provider.Models.Select(MapModel).ToArray());
    }

    private static AiProviderModelResponse MapModel(AiProviderModel model)
    {
        return new AiProviderModelResponse(
            model.Id,
            model.ModelId,
            model.DisplayName,
            model.Enabled,
            model.Kind.ToString());
    }
}
