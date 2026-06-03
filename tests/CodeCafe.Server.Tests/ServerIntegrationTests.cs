using CodeCafe.Mcp.Tools.Notes;
using CodeCafe.Server.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CodeCafe.Server.Tests;

public sealed class ServerIntegrationTests : IClassFixture<ServerTestFactory>
{
    private readonly ServerTestFactory _factory;

    public ServerIntegrationTests(ServerTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CombinedHost_ExposesApiAndMcpHealthEndpoints()
    {
        using var client = _factory.CreateClient();

        using var apiResponse = await client.GetAsync("/health/live");
        using var mcpResponse = await client.GetAsync("/mcp/health/live");

        apiResponse.EnsureSuccessStatusCode();
        mcpResponse.EnsureSuccessStatusCode();

        using var apiDocument = JsonDocument.Parse(await apiResponse.Content.ReadAsStringAsync());
        using var mcpDocument = JsonDocument.Parse(await mcpResponse.Content.ReadAsStringAsync());

        Assert.Equal("api", apiDocument.RootElement.GetProperty("adapter").GetString());
        Assert.Equal("mcp", mcpDocument.RootElement.GetProperty("adapter").GetString());
    }

    [Fact]
    public async Task CombinedHost_ExposesApiNotesAndAuthEndpoints()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Add(ServerTestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var publicNotesResponse = await client.GetAsync("/api/notes/public");
        publicNotesResponse.EnsureSuccessStatusCode();

        using var createResponse = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/notes", new
        {
            title = "Combined Host Notebook",
            visibility = "public"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    }

    [Fact]
    public async Task CombinedHost_ExposesOpenIdConfigurationAndRegistrationEndpoint()
    {
        using var client = _factory.CreateClient();

        using var configurationResponse = await client.GetAsync("/.well-known/openid-configuration");
        configurationResponse.EnsureSuccessStatusCode();
        using var configurationDocument = JsonDocument.Parse(await configurationResponse.Content.ReadAsStringAsync());
        Assert.EndsWith("/connect/register", configurationDocument.RootElement.GetProperty("registration_endpoint").GetString(), StringComparison.Ordinal);

        using var registrationResponse = await client.PostAsJsonAsync("/connect/register", new
        {
            client_name = "Bad Client",
            application_type = "native",
            token_endpoint_auth_method = "none",
            redirect_uris = new[] { "https://example.com/callback" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, registrationResponse.StatusCode);
        using var registrationDocument = JsonDocument.Parse(await registrationResponse.Content.ReadAsStringAsync());
        Assert.Equal("invalid_redirect_uri", registrationDocument.RootElement.GetProperty("error").GetString());

        using var metadataResponse = await client.GetAsync("/.well-known/oauth-protected-resource/mcp");
        metadataResponse.EnsureSuccessStatusCode();
        using var metadataDocument = JsonDocument.Parse(await metadataResponse.Content.ReadAsStringAsync());
        Assert.EndsWith("/mcp", metadataDocument.RootElement.GetProperty("resource").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CombinedHost_ExposesAuthenticatedNotesMcpSurface()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(ServerTestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(client.BaseAddress!, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            },
            client);

        await using var mcpClient = await McpClient.CreateAsync(transport);

        var tools = await mcpClient.ListToolsAsync();
        var resources = await mcpClient.ListResourcesAsync();
        var resourceTemplates = await mcpClient.ListResourceTemplatesAsync();
        var prompts = await mcpClient.ListPromptsAsync();

        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.ListNotebooks);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.ListItems);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.GetLimits);
        Assert.Contains(resources, resource => resource.Uri == "notes://guide");
        Assert.Contains(resourceTemplates, resource => resource.UriTemplate == "notebook://{slug}");
        Assert.Contains(prompts, prompt => prompt.Name == "notes.summarize_page");

        var listResult = await mcpClient.CallToolAsync(
            NotesMcpToolNames.ListNotebooks,
            new Dictionary<string, object?> { ["scope"] = "public" });
        Assert.Equal(1, listResult.StructuredContent!.Value.GetProperty("totalCount").GetInt32());

        var notebookResult = await mcpClient.CallToolAsync(
            NotesMcpToolNames.GetNotebook,
            new Dictionary<string, object?> { ["slug"] = "architecture-notes" });
        Assert.Equal("Architecture Notes", notebookResult.StructuredContent!.Value.GetProperty("title").GetString());

        var itemsResult = await mcpClient.CallToolAsync(
            NotesMcpToolNames.ListItems,
            new Dictionary<string, object?> { ["notebookSlug"] = "architecture-notes" });
        Assert.Contains(
            itemsResult.StructuredContent!.Value.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("path").GetString() == "guides/overview");

        var guideResult = await mcpClient.ReadResourceAsync("notes://guide");
        var guide = Assert.IsType<TextResourceContents>(Assert.Single(guideResult.Contents));
        Assert.Contains(NotesMcpToolNames.CreateUpload, guide.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CombinedHost_ReadinessFails_WhenServerIsDraining()
    {
        using var factory = new ServerTestFactory();
        var drainState = factory.Services.GetRequiredService<ServerDrainState>();
        _ = drainState.BeginDraining("test");

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("unhealthy", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("unhealthy", document.RootElement.GetProperty("checks").GetProperty("drain").GetProperty("status").GetString());
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
