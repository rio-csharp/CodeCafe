using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

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
    public async Task HealthReady_ReturnsAdapterStatus()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ready", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("api", document.RootElement.GetProperty("adapter").GetString());
    }

    [Fact]
    public async Task CsrfEndpoint_ReturnsRequestToken()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/auth/csrf");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Register_ReturnsAuthPayload()
    {
        using var client = CreateCookieClient();

        using var response = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/auth/register", new
        {
            email = "new.user@example.com",
            password = "Password123!",
            displayName = "New User"
        });

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("new.user@example.com", document.RootElement.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_ReturnsAuthPayload()
    {
        using var client = CreateCookieClient();

        using var response = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/auth/login", new
        {
            email = "yao@example.com",
            password = "Password123!"
        });

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("yao@example.com", document.RootElement.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Me_RequiresAuthentication()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WhenAuthenticated_ReturnsCurrentUser()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var response = await client.GetAsync("/api/auth/me");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("yao@example.com", document.RootElement.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Logout_RequiresAuthentication()
    {
        using var client = CreateCookieClient();

        using var response = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/auth/logout", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
    public async Task PublicNotebookItems_ReturnsItems()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/notes/public/architecture-notes/items");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("Overview", item.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PublicNotebookItem_ReturnsSingleItem()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/notes/public/architecture-notes/items/overview");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("overview", document.RootElement.GetProperty("slug").GetString());
    }

    [Fact]
    public async Task CreateNotebook_RequiresAuthentication()
    {
        using var client = CreateCookieClient();

        using var response = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/notes", new
        {
            title = "New Notebook",
            visibility = "public"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateNotebook_WhenAuthenticated_ReturnsCreatedNotebook()
    {
        using var client = CreateCookieClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var response = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/notes", new
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

    [Fact]
    public async Task FavoriteStatus_WhenAuthenticated_ReturnsFavoritePayload()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var response = await client.GetAsync("/api/notes/11111111-1111-1111-1111-111111111111/favorite");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, document.RootElement.GetProperty("favoriteCount").GetInt32());
        Assert.False(document.RootElement.GetProperty("isFavorited").GetBoolean());
    }

    [Fact]
    public async Task NotebookItems_WhenAuthenticated_ReturnsItemList()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var response = await client.GetAsync("/api/notes/11111111-1111-1111-1111-111111111111/items");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("Overview", item.GetProperty("title").GetString());
    }

    [Fact]
    public async Task CreateNotebookItem_WhenAuthenticated_ReturnsCreatedItem()
    {
        using var client = CreateCookieClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var response = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/notes/11111111-1111-1111-1111-111111111111/items", new
        {
            type = "page",
            title = "New Item",
            sortOrder = 2,
            contentJson = new
            {
                type = "doc"
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("New Item", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task DeleteNotebook_WhenAuthenticated_ReturnsNoContent()
    {
        using var client = CreateCookieClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var response = await SendWithCsrfAsync(client, HttpMethod.Delete, "/api/notes/11111111-1111-1111-1111-111111111111", new { });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private HttpClient CreateCookieClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
    }

    private static async Task<HttpResponseMessage> SendWithCsrfAsync(
        HttpClient client,
        HttpMethod method,
        string requestUri,
        object body)
    {
        var csrf = await GetCsrfTokenAsync(client);
        var request = new HttpRequestMessage(method, requestUri)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return await client.SendAsync(request);
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/auth/csrf");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Missing CSRF token.");
    }
}
