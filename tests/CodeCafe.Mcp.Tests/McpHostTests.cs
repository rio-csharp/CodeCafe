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
        Assert.Equal(16384, limitsResult.StructuredContent!.Value.GetProperty("maxInlineContentBytes").GetInt32());

        var guideResult = await mcpClient.ReadResourceAsync("notes://guide");
        var guide = Assert.IsType<TextResourceContents>(Assert.Single(guideResult.Contents));
        Assert.Contains(NotesMcpToolNames.AppendUploadChunk, guide.Text, StringComparison.Ordinal);

        var listResult = await mcpClient.CallToolAsync("notes_list_public_notebooks", new Dictionary<string, object?>());
        Assert.Equal(1, listResult.StructuredContent!.Value.GetProperty("TotalCount").GetInt32());

        var detailResult = await mcpClient.CallToolAsync(
            "notes_get_public_notebook",
            new Dictionary<string, object?> { ["slug"] = "architecture-notes" });
        Assert.Equal("Architecture Notes", detailResult.StructuredContent!.Value.GetProperty("Title").GetString());
    }
}
