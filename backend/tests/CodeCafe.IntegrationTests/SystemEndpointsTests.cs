using CodeCafe.Contracts.System;

namespace CodeCafe.IntegrationTests;

public sealed class SystemEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Health_returns_success()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Kubernetes_health_endpoints_return_success(string path)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task System_info_requires_authentication()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/system/info");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task System_info_returns_application_metadata_for_authenticated_users()
    {
        var client = factory.CreateClient();
        await client.LoginAsync();

        var response = await client.GetFromJsonAsync<SystemInfoResponse>("/api/system/info");

        Assert.NotNull(response);
        Assert.Equal("CodeCafe", response.Name);
        Assert.Equal("Testing", response.Environment);
        Assert.True(response.ServerTimeUtc <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Swagger_document_is_available()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
