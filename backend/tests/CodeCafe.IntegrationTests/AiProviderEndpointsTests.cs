using CodeCafe.Contracts.Ai;
using System.Net;
using System.Net.Http.Json;

namespace CodeCafe.IntegrationTests;

public sealed class AiProviderEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Built_in_provider_configuration_can_be_updated()
    {
        var client = factory.CreateClient();

        var providers = await client.GetFromJsonAsync<IReadOnlyCollection<AiProviderResponse>>("/api/ai/providers");

        Assert.NotNull(providers);
        Assert.Contains(providers, provider => provider.Name == "OpenAI");
        Assert.Contains(providers, provider => provider.Name == "DeepSeek");
        Assert.Contains(providers, provider => provider.Name == "Kimi");

        var openAi = providers.Single(provider => provider.Name == "OpenAI");

        Assert.True(openAi.BuiltIn);
        Assert.Empty(openAi.Models);

        var updateProviderResponse = await client.PutAsJsonAsync($"/api/ai/providers/{openAi.Id}", new UpsertAiProviderRequest(
            "OpenAI",
            "https://api.openai.com/v1",
            "sk-test",
            true));

        Assert.Equal(HttpStatusCode.OK, updateProviderResponse.StatusCode);

        var updatedProvider = await updateProviderResponse.Content.ReadFromJsonAsync<AiProviderResponse>();

        Assert.NotNull(updatedProvider);
        Assert.True(updatedProvider.Enabled);
        Assert.Equal("sk-test", updatedProvider.ApiKey);
    }

    [Fact]
    public async Task Provider_model_crud_flow_succeeds()
    {
        var client = factory.CreateClient();

        var createProviderResponse = await client.PostAsJsonAsync("/api/ai/providers", new UpsertAiProviderRequest(
            "Local Gateway",
            "http://localhost:9000/v1",
            null,
            true));

        Assert.Equal(HttpStatusCode.Created, createProviderResponse.StatusCode);

        var provider = await createProviderResponse.Content.ReadFromJsonAsync<AiProviderResponse>();

        Assert.NotNull(provider);
        Assert.False(provider.BuiltIn);
        Assert.Empty(provider.Models);

        var createModelResponse = await client.PostAsJsonAsync($"/api/ai/providers/{provider.Id}/models", new UpsertAiProviderModelRequest(
            "local-coder",
            "Local Coder",
            true,
            "Custom"));

        Assert.Equal(HttpStatusCode.Created, createModelResponse.StatusCode);

        var model = await createModelResponse.Content.ReadFromJsonAsync<AiProviderModelResponse>();

        Assert.NotNull(model);
        Assert.Equal("local-coder", model.ModelId);
        Assert.Equal("Custom", model.Kind);

        var updateModelResponse = await client.PutAsJsonAsync($"/api/ai/providers/{provider.Id}/models/{model.Id}", new UpsertAiProviderModelRequest(
            "local-coder-v2",
            "Local Coder V2",
            false,
            "Official"));

        Assert.Equal(HttpStatusCode.OK, updateModelResponse.StatusCode);

        var updatedModel = await updateModelResponse.Content.ReadFromJsonAsync<AiProviderModelResponse>();

        Assert.NotNull(updatedModel);
        Assert.Equal("local-coder-v2", updatedModel.ModelId);
        Assert.False(updatedModel.Enabled);
        Assert.Equal("Official", updatedModel.Kind);

        var populatedProvider = await client.GetFromJsonAsync<AiProviderResponse>($"/api/ai/providers/{provider.Id}");

        Assert.NotNull(populatedProvider);
        Assert.Single(populatedProvider.Models);

        var deleteModelResponse = await client.DeleteAsync($"/api/ai/providers/{provider.Id}/models/{model.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteModelResponse.StatusCode);

        var deleteProviderResponse = await client.DeleteAsync($"/api/ai/providers/{provider.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteProviderResponse.StatusCode);
    }

    [Fact]
    public async Task Missing_provider_returns_not_found_for_model_create()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/ai/providers/{Guid.NewGuid()}/models", new UpsertAiProviderModelRequest(
            "missing",
            "Missing",
            true,
            "Custom"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
