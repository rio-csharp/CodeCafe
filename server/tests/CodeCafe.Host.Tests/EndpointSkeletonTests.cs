using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CodeCafe.Host.Tests;

public sealed class EndpointSkeletonTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    // Contract spot-check: an anonymously reachable mapped route returns 501;
    // an unmapped one would 404, a protected one would 401.
    // GET /api/notebooks/{idOrSlug} and the page reads are implemented now, so they no longer
    // answer 501 here; their mapping is covered by OpenApiContractTests instead.
    [Theory]
    [InlineData("GET", "/api/notebooks/my-first-notes/export")]
    public async Task Contract_Route_Is_Mapped(string method, string path)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PATCH" or "PUT")
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/auth/me")]
    [InlineData("PATCH", "/api/auth/me")]
    [InlineData("POST", "/api/auth/change-password")]
    [InlineData("GET", "/api/notebooks")]
    [InlineData("POST", "/api/notebooks")]
    [InlineData("POST", "/api/notebooks/import")]
    [InlineData("PATCH", "/api/notebooks/my-first-notes")]
    [InlineData("DELETE", "/api/notebooks/my-first-notes")]
    [InlineData("POST", "/api/notebooks/my-first-notes/slug")]
    [InlineData("POST", "/api/notebooks/my-first-notes/access-code")]
    [InlineData("POST", "/api/notebooks/my-first-notes/favorite")]
    [InlineData("POST", "/api/notebooks/my-first-notes/shares")]
    [InlineData("DELETE", "/api/notebooks/my-first-notes/shares/00000000-0000-0000-0000-000000000002")]
    [InlineData("PUT", "/api/notebooks/my-first-notes/tags")]
    [InlineData("POST", "/api/notebooks/my-first-notes/pages")]
    [InlineData("PATCH", "/api/pages/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/pages/00000000-0000-0000-0000-000000000001/move")]
    [InlineData("DELETE", "/api/pages/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/pages/00000000-0000-0000-0000-000000000001/favorite")]
    [InlineData("GET", "/api/pages/favorites")]
    [InlineData("POST", "/api/pages/00000000-0000-0000-0000-000000000001/shares")]
    [InlineData("DELETE", "/api/pages/00000000-0000-0000-0000-000000000001/shares/00000000-0000-0000-0000-000000000002")]
    [InlineData("POST", "/api/pages/00000000-0000-0000-0000-000000000001/blocks")]
    [InlineData("POST", "/api/pages/00000000-0000-0000-0000-000000000001/blocks/batch")]
    [InlineData("PATCH", "/api/pages/00000000-0000-0000-0000-000000000001/blocks/00000000-0000-0000-0000-000000000002")]
    [InlineData("DELETE", "/api/pages/00000000-0000-0000-0000-000000000001/blocks/00000000-0000-0000-0000-000000000002")]
    [InlineData("POST", "/api/pages/00000000-0000-0000-0000-000000000001/blocks/00000000-0000-0000-0000-000000000002/move")]
    [InlineData("GET", "/api/pages/00000000-0000-0000-0000-000000000001/revisions")]
    [InlineData("POST", "/api/pages/00000000-0000-0000-0000-000000000001/revisions/restore")]
    [InlineData("GET", "/api/pages/00000000-0000-0000-0000-000000000001/blocks/00000000-0000-0000-0000-000000000002/revisions")]
    [InlineData("POST", "/api/pages/00000000-0000-0000-0000-000000000001/blocks/00000000-0000-0000-0000-000000000002/revisions/restore")]
    [InlineData("GET", "/api/trash")]
    [InlineData("POST", "/api/trash/00000000-0000-0000-0000-000000000001/restore")]
    [InlineData("DELETE", "/api/trash/00000000-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/trash")]
    [InlineData("GET", "/api/notebooks/my-first-notes/trash")]
    [InlineData("POST", "/api/trash/pages/00000000-0000-0000-0000-000000000001/restore")]
    [InlineData("DELETE", "/api/trash/pages/00000000-0000-0000-0000-000000000001")]
    [InlineData("GET", "/api/search?q=test")]
    [InlineData("POST", "/api/notebooks/my-first-notes/ai/chat")]
    [InlineData("GET", "/api/notebooks/slugs/my-slug")]
    [InlineData("POST", "/mcp")]
    public async Task Authorized_Route_Challenges_When_Anonymous(string method, string path)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PATCH" or "PUT")
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_Check_Is_Alive(string path)
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_Challenge_Returns_Bearer_Header_And_Error_Envelope()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/notebooks", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header =>
            header.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.False(json.GetProperty("isSuccess").GetBoolean());
        Assert.Equal("unauthorized", json.GetProperty("error").GetProperty("code").GetString());
        // Enums travel as strings on the wire.
        Assert.Equal("Unauthorized", json.GetProperty("error").GetProperty("kind").GetString());
    }

    [Fact]
    public async Task Root_Challenges_When_Anonymous()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
