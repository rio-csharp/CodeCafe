using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CodeCafe.Api.Tests;

public sealed class ApiEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public ApiEndpointTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthLive_ReturnsAdapterStatus()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ok", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("api", document.RootElement.GetProperty("adapter").GetString());
    }

    [Fact]
    public async Task PublicNotesEndpoint_ReturnsNotebookSummaries()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/notes/public");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var notebook = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("architecture-notes", notebook.GetProperty("slug").GetString());
        Assert.Equal("Architecture Notes", notebook.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PublicNotebookDetail_ReturnsNotebookPayload()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/notes/public/architecture-notes");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("architecture-notes", document.RootElement.GetProperty("slug").GetString());
        Assert.Equal("Refactor plan", document.RootElement.GetProperty("description").GetString());
    }

    [Fact]
    public async Task MyNotes_RequiresAuthentication()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/notes/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MyNotes_WhenAuthenticated_ReturnsOwnedSummaries()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var response = await client.GetAsync("/api/notes/mine");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var notebook = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("my-notes", notebook.GetProperty("slug").GetString());
        Assert.True(notebook.GetProperty("canEdit").GetBoolean());
    }

    [Fact]
    public async Task NotebookByGuid_WhenAuthenticated_ReturnsDetail()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var response = await client.GetAsync("/api/notes/11111111-1111-1111-1111-111111111111");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Architecture Notes", document.RootElement.GetProperty("title").GetString());
        Assert.True(document.RootElement.GetProperty("canEdit").GetBoolean());
    }

    [Fact]
    public async Task NotebookBySlug_ReturnsDetailForAnonymousReader()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/notes/architecture-notes");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("architecture-notes", document.RootElement.GetProperty("slug").GetString());
        Assert.False(document.RootElement.GetProperty("canEdit").GetBoolean());
    }

    [Fact]
    public async Task CreateNotebook_RequiresAuthentication()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/notes", new
        {
            title = "New Notebook",
            visibility = "public"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateNotebook_WhenAuthenticated_ReturnsCreatedNotebook()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var response = await client.PostAsJsonAsync("/api/notes", new
        {
            title = "New Notebook",
            description = "Created from endpoint test",
            visibility = "public"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("New Notebook", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("public", document.RootElement.GetProperty("visibility").GetString());
        Assert.Equal("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", document.RootElement.GetProperty("ownerId").GetGuid().ToString());
    }
}
