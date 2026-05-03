namespace CodeCafe.Api.Controllers;

using CodeCafe.Application.Ai;
using CodeCafe.Contracts.Ai;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/ai/providers")]
[Tags("AI Providers")]
public sealed class AiProvidersController(IAiProviderConfigurationService service) : ControllerBase
{
    [HttpGet(Name = "ListAiProviders")]
    public async Task<ActionResult<IReadOnlyCollection<AiProviderResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        var providers = await service.ListProvidersAsync(cancellationToken);

        return Ok(providers);
    }

    [HttpGet("{providerId:guid}", Name = "GetAiProvider")]
    public async Task<ActionResult<AiProviderResponse>> GetAsync(Guid providerId, CancellationToken cancellationToken)
    {
        var provider = await service.GetProviderAsync(providerId, cancellationToken);

        return provider is null ? NotFound() : Ok(provider);
    }

    [HttpPost(Name = "CreateAiProvider")]
    public async Task<ActionResult<AiProviderResponse>> CreateAsync(
        UpsertAiProviderRequest request,
        CancellationToken cancellationToken)
    {
        var provider = await service.CreateProviderAsync(request, cancellationToken);

        return Created($"/api/ai/providers/{provider.Id}", provider);
    }

    [HttpPut("{providerId:guid}", Name = "UpdateAiProvider")]
    public async Task<ActionResult<AiProviderResponse>> UpdateAsync(
        Guid providerId,
        UpsertAiProviderRequest request,
        CancellationToken cancellationToken)
    {
        var provider = await service.UpdateProviderAsync(providerId, request, cancellationToken);

        return provider is null ? NotFound() : Ok(provider);
    }

    [HttpDelete("{providerId:guid}", Name = "DeleteAiProvider")]
    public async Task<IActionResult> DeleteAsync(Guid providerId, CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteProviderAsync(providerId, cancellationToken);

        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{providerId:guid}/models", Name = "CreateAiProviderModel")]
    public async Task<ActionResult<AiProviderModelResponse>> CreateModelAsync(
        Guid providerId,
        UpsertAiProviderModelRequest request,
        CancellationToken cancellationToken)
    {
        var model = await service.CreateModelAsync(providerId, request, cancellationToken);

        return model is null
            ? NotFound()
            : Created($"/api/ai/providers/{providerId}/models/{model.Id}", model);
    }

    [HttpPut("{providerId:guid}/models/{modelId:guid}", Name = "UpdateAiProviderModel")]
    public async Task<ActionResult<AiProviderModelResponse>> UpdateModelAsync(
        Guid providerId,
        Guid modelId,
        UpsertAiProviderModelRequest request,
        CancellationToken cancellationToken)
    {
        var model = await service.UpdateModelAsync(providerId, modelId, request, cancellationToken);

        return model is null ? NotFound() : Ok(model);
    }

    [HttpDelete("{providerId:guid}/models/{modelId:guid}", Name = "DeleteAiProviderModel")]
    public async Task<IActionResult> DeleteModelAsync(
        Guid providerId,
        Guid modelId,
        CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteModelAsync(providerId, modelId, cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}
