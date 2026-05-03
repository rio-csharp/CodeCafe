namespace CodeCafe.Api.Endpoints;

using CodeCafe.Application.Ai;
using CodeCafe.Contracts.Ai;

public static class AiProviderEndpoints
{
    public static IEndpointRouteBuilder MapAiProviderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai/providers")
            .WithTags("AI Providers");

        group.MapGet("/", async (
            IAiProviderConfigurationService service,
            CancellationToken cancellationToken) =>
        {
            var providers = await service.ListProvidersAsync(cancellationToken);

            return Results.Ok(providers);
        })
        .WithName("ListAiProviders");

        group.MapGet("/{providerId:guid}", async (
            Guid providerId,
            IAiProviderConfigurationService service,
            CancellationToken cancellationToken) =>
        {
            var provider = await service.GetProviderAsync(providerId, cancellationToken);

            return provider is null ? Results.NotFound() : Results.Ok(provider);
        })
        .WithName("GetAiProvider");

        group.MapPost("/", async (
            UpsertAiProviderRequest request,
            IAiProviderConfigurationService service,
            CancellationToken cancellationToken) =>
        {
            var provider = await service.CreateProviderAsync(request, cancellationToken);

            return Results.Created($"/api/ai/providers/{provider.Id}", provider);
        })
        .WithName("CreateAiProvider");

        group.MapPut("/{providerId:guid}", async (
            Guid providerId,
            UpsertAiProviderRequest request,
            IAiProviderConfigurationService service,
            CancellationToken cancellationToken) =>
        {
            var provider = await service.UpdateProviderAsync(providerId, request, cancellationToken);

            return provider is null ? Results.NotFound() : Results.Ok(provider);
        })
        .WithName("UpdateAiProvider");

        group.MapDelete("/{providerId:guid}", async (
            Guid providerId,
            IAiProviderConfigurationService service,
            CancellationToken cancellationToken) =>
        {
            var deleted = await service.DeleteProviderAsync(providerId, cancellationToken);

            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteAiProvider");

        group.MapPost("/{providerId:guid}/models", async (
            Guid providerId,
            UpsertAiProviderModelRequest request,
            IAiProviderConfigurationService service,
            CancellationToken cancellationToken) =>
        {
            var model = await service.CreateModelAsync(providerId, request, cancellationToken);

            return model is null
                ? Results.NotFound()
                : Results.Created($"/api/ai/providers/{providerId}/models/{model.Id}", model);
        })
        .WithName("CreateAiProviderModel");

        group.MapPut("/{providerId:guid}/models/{modelId:guid}", async (
            Guid providerId,
            Guid modelId,
            UpsertAiProviderModelRequest request,
            IAiProviderConfigurationService service,
            CancellationToken cancellationToken) =>
        {
            var model = await service.UpdateModelAsync(providerId, modelId, request, cancellationToken);

            return model is null ? Results.NotFound() : Results.Ok(model);
        })
        .WithName("UpdateAiProviderModel");

        group.MapDelete("/{providerId:guid}/models/{modelId:guid}", async (
            Guid providerId,
            Guid modelId,
            IAiProviderConfigurationService service,
            CancellationToken cancellationToken) =>
        {
            var deleted = await service.DeleteModelAsync(providerId, modelId, cancellationToken);

            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteAiProviderModel");

        return app;
    }
}
