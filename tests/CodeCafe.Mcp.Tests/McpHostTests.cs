using CodeCafe.Application.Notes;
using CodeCafe.Mcp.Tools.Notes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Net;
using System.Text.Json;

namespace CodeCafe.Mcp.Tests;

public sealed class McpHostTests : IClassFixture<McpTestFactory>
{
    private readonly McpTestFactory _factory;

    public McpHostTests(McpTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsAdapterStatus()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/mcp/health/live");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ok", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("mcp", document.RootElement.GetProperty("adapter").GetString());
    }

    [Fact]
    public async Task McpTransportRoute_IsMapped()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsync("/mcp", new StringContent(string.Empty));

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task McpToolsResourcesAndPrompts_AreExposedByTheStandaloneHost()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(McpTestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

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

        Assert.Contains(tools, tool => tool.Name == "diagnostics_status");
        Assert.Contains(tools, tool => tool.Name == "notes_list_public_notebooks");
        Assert.Contains(tools, tool => tool.Name == "notes_get_public_notebook");
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.ListNotebooks);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.GetLimits);
        Assert.Contains(resources, resource => resource.Uri == "notes://guide");
        Assert.Contains(resourceTemplates, resource => resource.UriTemplate == "notebook://{slug}");
        Assert.Contains(prompts, prompt => prompt.Name == "notes.summarize_page");

        var limitsResult = await mcpClient.CallToolAsync(NotesMcpToolNames.GetLimits, new Dictionary<string, object?>());
        Assert.Equal(131072, limitsResult.StructuredContent!.Value.GetProperty("maxInlineContentBytes").GetInt32());
        Assert.Equal(64, limitsResult.StructuredContent!.Value.GetProperty("maxTipTapDepth").GetInt32());
        Assert.Equal(5000, limitsResult.StructuredContent!.Value.GetProperty("maxTipTapNodeCount").GetInt32());
        Assert.Equal(200000, limitsResult.StructuredContent!.Value.GetProperty("maxTipTapTextLength").GetInt32());

        var guideResult = await mcpClient.ReadResourceAsync("notes://guide");
        var guide = Assert.IsType<TextResourceContents>(Assert.Single(guideResult.Contents));
        Assert.Contains(NotesMcpToolNames.AppendUploadChunk, guide.Text, StringComparison.Ordinal);
        Assert.Contains("TipTap node and text limits", guide.Text, StringComparison.Ordinal);

        var listResult = await mcpClient.CallToolAsync("notes_list_public_notebooks", new Dictionary<string, object?>());
        Assert.Equal(1, listResult.StructuredContent!.Value.GetProperty("totalCount").GetInt32());

        var detailResult = await mcpClient.CallToolAsync(
            "notes_get_public_notebook",
            new Dictionary<string, object?> { ["slug"] = "architecture-notes" });
        Assert.Equal("Architecture Notes", detailResult.StructuredContent!.Value.GetProperty("title").GetString());
    }

    [Fact]
    public async Task McpUpload_AllowsChunksLargerThanTheOldDefaultLimit()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(McpTestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(client.BaseAddress!, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            },
            client);

        await using var mcpClient = await McpClient.CreateAsync(transport);

        var createResult = await mcpClient.CallToolAsync(NotesMcpToolNames.CreateUpload, new Dictionary<string, object?>
        {
            ["fileName"] = "large-page.md",
            ["mediaType"] = "text/markdown"
        });
        var uploadId = createResult.StructuredContent!.Value.GetProperty("uploadId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(uploadId));

        var chunkText = new string('a', 20_000);
        var appendResult = await mcpClient.CallToolAsync(NotesMcpToolNames.AppendUploadChunk, new Dictionary<string, object?>
        {
            ["uploadId"] = uploadId,
            ["chunkText"] = chunkText
        });

        Assert.False(appendResult.IsError ?? false);
        Assert.Equal(20_000, appendResult.StructuredContent!.Value.GetProperty("bytesReceived").GetInt32());
    }

    [Fact]
    public async Task DiscardUpload_IsIdempotent()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(McpTestAuthHandler.UserIdHeader, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(client.BaseAddress!, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            },
            client);

        await using var mcpClient = await McpClient.CreateAsync(transport);

        var createResult = await mcpClient.CallToolAsync(NotesMcpToolNames.CreateUpload, new Dictionary<string, object?>());
        var uploadId = createResult.StructuredContent!.Value.GetProperty("uploadId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(uploadId));

        var firstDiscard = await mcpClient.CallToolAsync(NotesMcpToolNames.DiscardUpload, new Dictionary<string, object?>
        {
            ["uploadId"] = uploadId
        });
        var secondDiscard = await mcpClient.CallToolAsync(NotesMcpToolNames.DiscardUpload, new Dictionary<string, object?>
        {
            ["uploadId"] = uploadId
        });

        Assert.False(firstDiscard.IsError ?? false);
        Assert.Equal("discarded", firstDiscard.StructuredContent!.Value.GetProperty("result").GetString());
        Assert.False(secondDiscard.IsError ?? false);
        Assert.Equal("already_absent", secondDiscard.StructuredContent!.Value.GetProperty("result").GetString());
    }

    [Fact]
    public void FailureResult_IncludesFieldAndDetails()
    {
        var result = NotesMcpResultMapper.Failure(new NotesError(
            NotesFailureKind.Validation,
            "invalid_tiptap_document",
            "ContentJson contains too many nodes.",
            "contentJson",
            new Dictionary<string, object?>
            {
                ["maxTipTapNodeCount"] = 5000,
                ["actualTipTapNodeCount"] = 5001
            }));

        var content = result.StructuredContent!.Value;
        Assert.True(result.IsError);
        Assert.Equal("invalid_tiptap_document", content.GetProperty("code").GetString());
        Assert.Equal("contentJson", content.GetProperty("field").GetString());
        Assert.Equal(5000, content.GetProperty("details").GetProperty("maxTipTapNodeCount").GetInt32());
        Assert.Equal(5001, content.GetProperty("details").GetProperty("actualTipTapNodeCount").GetInt32());
    }
}
