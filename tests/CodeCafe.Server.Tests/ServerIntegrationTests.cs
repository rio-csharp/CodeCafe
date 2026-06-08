using CodeCafe.Ai.Drafts;
using CodeCafe.Mcp.Tools.Notes;
using CodeCafe.Server.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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
    public async Task CombinedHost_ExposesAiStatusWhenAiIsDisabled()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/ai/status");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(document.RootElement.GetProperty("enabled").GetBoolean());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("endpointPath").ValueKind);
    }

    [Fact]
    public async Task CombinedHost_ExposesAiStatusAtConfiguredPath()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ai:StatusEndpointPath"] = "/internal/ai/status"
                });
            });
        });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/internal/ai/status");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(document.RootElement.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task CombinedHost_ExposesAiStatusWhenAiIsEnabled()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ai:Enabled"] = "true",
                    ["Ai:ApiKey"] = "test-key",
                    ["Ai:Model"] = "test-model"
                });
            });
        });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/ai/status");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.GetProperty("enabled").GetBoolean());
        Assert.Equal("/api/ai/assistant", document.RootElement.GetProperty("endpointPath").GetString());
        Assert.Equal("/api/ai/drafts", document.RootElement.GetProperty("draftEndpointPath").GetString());
    }

    [Fact]
    public async Task CombinedHost_AiAssistantEndpoint_IsNotMappedWhenAiIsDisabled()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/ai/assistant",
            new { messages = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CombinedHost_AiAssistantEndpoint_RequiresAuthenticationWhenAiIsEnabled()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ai:Enabled"] = "true",
                    ["Ai:ApiKey"] = "test-key",
                    ["Ai:Model"] = "test-model"
                });
            });
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/ai/assistant",
            new { messages = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CombinedHost_AiDraftEndpoint_GeneratesDraftFromNotebookContext()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ai:Enabled"] = "true",
                    ["Ai:ApiKey"] = "test-key",
                    ["Ai:Model"] = "test-model"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiNoteDraftGenerator>();
                services.AddSingleton<RecordingDraftGenerator>();
                services.AddSingleton<IAiNoteDraftGenerator>(serviceProvider =>
                    serviceProvider.GetRequiredService<RecordingDraftGenerator>());
            });
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Add(ServerTestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/ai/drafts",
            new
            {
                notebookSlug = "architecture-notes",
                activePagePath = "guides/overview",
                intent = "rewrite",
                prompt = "Turn this into a short implementation checklist.",
                locale = "en"
            });

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, responseBody);
        using var document = JsonDocument.Parse(responseBody);
        Assert.Equal("# Implementation Checklist", document.RootElement.GetProperty("markdown").GetString()!.Split('\n')[0]);
        Assert.Equal("Implementation Checklist", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("rewrite", document.RootElement.GetProperty("intent").GetString());
        Assert.Equal("architecture-notes", document.RootElement.GetProperty("notebookSlug").GetString());
        Assert.Equal("guides/overview", document.RootElement.GetProperty("pagePath").GetString());

        var generator = factory.Services.GetRequiredService<RecordingDraftGenerator>();
        var context = Assert.Single(generator.Contexts);
        Assert.Equal("rewrite", context.Intent);
        Assert.Equal("Architecture Notes", context.Notebook.Title);
        Assert.Equal("Overview", context.ActivePage?.Title);
        Assert.Equal("Turn this into a short implementation checklist.", context.Prompt);
    }

    [Fact]
    public async Task CombinedHost_AiDraftEndpoint_ReturnsProblemDetailsWhenGeneratorFails()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ai:Enabled"] = "true",
                    ["Ai:ApiKey"] = "test-key",
                    ["Ai:Model"] = "test-model"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiNoteDraftGenerator>();
                services.AddSingleton<IAiNoteDraftGenerator, ThrowingDraftGenerator>();
            });
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Add(ServerTestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/ai/drafts",
            new
            {
                notebookSlug = "architecture-notes",
                activePagePath = "guides/overview",
                intent = "rewrite",
                prompt = "Turn this into a short implementation checklist.",
                locale = "en"
            });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ai_draft_generation_failed", document.RootElement.GetProperty("code").GetString());
        Assert.True(document.RootElement.GetProperty("retryable").GetBoolean());
    }

    [Fact]
    public async Task CombinedHost_AiDraftEndpoint_NormalizesUnknownIntentAndAllowsNotebookWideDrafts()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ai:Enabled"] = "true",
                    ["Ai:ApiKey"] = "test-key",
                    ["Ai:Model"] = "test-model"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiNoteDraftGenerator>();
                services.AddSingleton<RecordingDraftGenerator>();
                services.AddSingleton<IAiNoteDraftGenerator>(serviceProvider =>
                    serviceProvider.GetRequiredService<RecordingDraftGenerator>());
            });
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Add(ServerTestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/ai/drafts",
            new
            {
                notebookSlug = "architecture-notes",
                activePagePath = (string?)null,
                intent = "brainstorm",
                prompt = "Draft a notebook-level follow-up note.",
                locale = "en"
            });

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, responseBody);
        using var document = JsonDocument.Parse(responseBody);
        Assert.Equal("custom", document.RootElement.GetProperty("intent").GetString());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("pagePath").ValueKind);

        var generator = factory.Services.GetRequiredService<RecordingDraftGenerator>();
        var context = Assert.Single(generator.Contexts);
        Assert.Equal("custom", context.Intent);
        Assert.Null(context.ActivePage);
    }

    [Fact]
    public async Task CombinedHost_AiDraftEndpoint_ReturnsNotFoundWhenActivePagePathDoesNotExist()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ai:Enabled"] = "true",
                    ["Ai:ApiKey"] = "test-key",
                    ["Ai:Model"] = "test-model"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiNoteDraftGenerator>();
                services.AddSingleton<RecordingDraftGenerator>();
                services.AddSingleton<IAiNoteDraftGenerator>(serviceProvider =>
                    serviceProvider.GetRequiredService<RecordingDraftGenerator>());
            });
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Add(ServerTestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/ai/drafts",
            new
            {
                notebookSlug = "architecture-notes",
                activePagePath = "guides/missing",
                intent = "summarize",
                prompt = "Summarize this page.",
                locale = "en"
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("notebook_item_not_found", document.RootElement.GetProperty("code").GetString());

        var generator = factory.Services.GetRequiredService<RecordingDraftGenerator>();
        Assert.Empty(generator.Contexts);
    }

    [Fact]
    public async Task CombinedHost_AiDraftEndpoint_ReturnsProblemDetailsWhenGeneratorReturnsEmptyDraft()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Ai:Enabled"] = "true",
                    ["Ai:ApiKey"] = "test-key",
                    ["Ai:Model"] = "test-model"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiNoteDraftGenerator>();
                services.AddSingleton<IAiNoteDraftGenerator, EmptyDraftGenerator>();
            });
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Add(ServerTestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var response = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/ai/drafts",
            new
            {
                notebookSlug = "architecture-notes",
                activePagePath = "guides/overview",
                intent = "summarize",
                prompt = "Summarize this page.",
                locale = "en"
            });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("empty_ai_draft", document.RootElement.GetProperty("code").GetString());
        Assert.True(document.RootElement.GetProperty("retryable").GetBoolean());
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
    public async Task CombinedHost_HttpMarkdownUpload_CanBeDiscardedThroughMcp()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Add(ServerTestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("# Uploaded title\n\nBody"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        form.Add(fileContent, "file", "uploaded-note.md");

        var csrf = await GetCsrfTokenAsync(client);
        var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "/api/notes/uploads/markdown")
        {
            Content = form
        };
        uploadRequest.Headers.Add("X-CSRF-TOKEN", csrf);

        using var uploadResponse = await client.SendAsync(uploadRequest);
        var uploadBody = await uploadResponse.Content.ReadAsStringAsync();
        Assert.True(uploadResponse.IsSuccessStatusCode, uploadBody);
        using var uploadDocument = JsonDocument.Parse(uploadBody);
        var uploadId = uploadDocument.RootElement.GetProperty("uploadId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(uploadId));
        Assert.Equal("text/markdown", uploadDocument.RootElement.GetProperty("mediaType").GetString());

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(client.BaseAddress!, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            },
            client);

        await using var mcpClient = await McpClient.CreateAsync(transport);
        var discardResult = await mcpClient.CallToolAsync(
            NotesMcpToolNames.DiscardUpload,
            new Dictionary<string, object?> { ["uploadId"] = uploadId });

        Assert.False(discardResult.IsError ?? false);
        Assert.Equal("discarded", discardResult.StructuredContent!.Value.GetProperty("result").GetString());

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/notes/uploads/{uploadId}");
        deleteRequest.Headers.Add("X-CSRF-TOKEN", csrf);
        using var secondDelete = await client.SendAsync(deleteRequest);
        secondDelete.EnsureSuccessStatusCode();
        using var deleteDocument = JsonDocument.Parse(await secondDelete.Content.ReadAsStringAsync());
        Assert.Equal("already_absent", deleteDocument.RootElement.GetProperty("result").GetString());
    }

    [Fact]
    public async Task CombinedHost_CanCreatePageFromUploadedMarkdown()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Add(ServerTestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var csrf = await GetCsrfTokenAsync(client);
        var uploadId = await UploadMarkdownAsync(client, csrf, "# Imported title\n\nImported body");

        using var createResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/notes/notebooks/architecture-notes/pages/import-markdown",
            new
            {
                title = "Imported Overview",
                uploadId,
                includeContent = true
            });

        var responseBody = await createResponse.Content.ReadAsStringAsync();
        Assert.True(createResponse.StatusCode == HttpStatusCode.Created, responseBody);
        using var document = JsonDocument.Parse(responseBody);
        Assert.Equal("Imported Overview", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("architecture-notes", document.RootElement.GetProperty("notebookSlug").GetString());
        Assert.Equal("tiptap_json", document.RootElement.GetProperty("contentFormat").GetString());
        Assert.True(document.RootElement.GetProperty("contentIncluded").GetBoolean());
        Assert.True(document.RootElement.GetProperty("contentJsonBytes").GetInt32() > 0);
        Assert.True(document.RootElement.GetProperty("tipTapNodeCount").GetInt32() > 0);
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("updatedAtUtc").GetString()));
        var firstNode = document.RootElement.GetProperty("contentJson").GetProperty("content").EnumerateArray().First();
        Assert.Equal("heading", firstNode.GetProperty("type").GetString());
    }

    [Fact]
    public async Task CombinedHost_MarkdownUploadErrors_ReturnProblemDetails()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Add(ServerTestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var csrf = await GetCsrfTokenAsync(client);
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("not markdown"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(fileContent, "file", "notes.txt");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/notes/uploads/markdown")
        {
            Content = form
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("unsupported_upload_media_type", document.RootElement.GetProperty("code").GetString());
        Assert.Equal("file", document.RootElement.GetProperty("field").GetString());
        Assert.False(document.RootElement.GetProperty("retryable").GetBoolean());
        Assert.True(document.RootElement.TryGetProperty("details", out _));
    }

    [Fact]
    public async Task CombinedHost_CanAppendUploadedMarkdownToExistingPage()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Add(ServerTestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var csrf = await GetCsrfTokenAsync(client);
        var uploadId = await UploadMarkdownAsync(client, csrf, "## Extra section\n\nMore body");

        using var appendResponse = await SendWithCsrfAsync(
            client,
            HttpMethod.Post,
            "/api/notes/notebooks/architecture-notes/pages/guides/overview/append-markdown",
            new
            {
                uploadId,
                includeContent = true
            });

        var responseBody = await appendResponse.Content.ReadAsStringAsync();
        Assert.True(appendResponse.IsSuccessStatusCode, responseBody);
        using var document = JsonDocument.Parse(responseBody);
        Assert.Equal("Overview", document.RootElement.GetProperty("title").GetString());
        Assert.True(document.RootElement.GetProperty("contentIncluded").GetBoolean());
        var content = document.RootElement.GetProperty("contentJson").GetProperty("content").EnumerateArray().ToList();
        Assert.True(content.Count >= 2);
        Assert.Equal("heading", content[1].GetProperty("type").GetString());
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

    private static async Task<string> UploadMarkdownAsync(HttpClient client, string csrf, string markdown)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(markdown));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/markdown");
        form.Add(fileContent, "file", "upload.md");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/notes/uploads/markdown")
        {
            Content = form
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("uploadId").GetString()
            ?? throw new InvalidOperationException("Missing uploadId.");
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/auth/csrf");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Missing CSRF token.");
    }

    private sealed class RecordingDraftGenerator : IAiNoteDraftGenerator
    {
        public List<AiNoteDraftGenerationContext> Contexts { get; } = [];

        public Task<AiNoteDraftResult> GenerateDraftAsync(
            AiNoteDraftGenerationContext context,
            CancellationToken cancellationToken)
        {
            Contexts.Add(context);
            return Task.FromResult(new AiNoteDraftResult("""
                # Implementation Checklist

                - Verify the current overview.
                - Cite `architecture-notes/guides/overview`.
            """));
        }
    }

    private sealed class ThrowingDraftGenerator : IAiNoteDraftGenerator
    {
        public Task<AiNoteDraftResult> GenerateDraftAsync(
            AiNoteDraftGenerationContext context,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Provider unavailable.");
        }
    }

    private sealed class EmptyDraftGenerator : IAiNoteDraftGenerator
    {
        public Task<AiNoteDraftResult> GenerateDraftAsync(
            AiNoteDraftGenerationContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new AiNoteDraftResult("   "));
        }
    }
}
