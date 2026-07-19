using CodeCafe.Modules.Notes.Application.Notes;
using CodeCafe.Modules.Mcp.Tools.Notes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Net;
using System.Text.Json;

namespace CodeCafe.Modules.Mcp.Tests;

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
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.PrepareHttpUpload);
        var createPageTool = Assert.Single(tools, tool => tool.Name == NotesMcpToolNames.CreatePage);
        var updatePageTool = Assert.Single(tools, tool => tool.Name == NotesMcpToolNames.UpdatePageContent);
        var renameItemTool = Assert.Single(tools, tool => tool.Name == NotesMcpToolNames.RenameItem);
        Assert.Contains("maxPageContentBytes", createPageTool.Description ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("maxTipTapNodeCount", updatePageTool.Description ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("maxTipTapDepth", updatePageTool.Description ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("page/<path>", renameItemTool.Description ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains(resources, resource => resource.Uri == "notes://guide");
        Assert.Contains(resourceTemplates, resource => resource.UriTemplate == "notebook://{slug}");
        Assert.Contains(prompts, prompt => prompt.Name == "notes.summarize_page");
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.ReplaceBlockAtIndex);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.InsertBlocksAtIndex);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.DeleteBlockAtIndex);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.ReplaceText);

        var limitsResult = await mcpClient.CallToolAsync(NotesMcpToolNames.GetLimits, new Dictionary<string, object?>());
        Assert.Equal(131072, limitsResult.StructuredContent!.Value.GetProperty("maxInlineContentBytes").GetInt32());
        Assert.Equal(4194304, limitsResult.StructuredContent!.Value.GetProperty("maxHttpUploadBytes").GetInt32());
        Assert.Equal(900, limitsResult.StructuredContent!.Value.GetProperty("uploadIdleTimeoutSeconds").GetInt32());
        Assert.Equal(64, limitsResult.StructuredContent!.Value.GetProperty("maxTipTapDepth").GetInt32());
        Assert.Equal(5000, limitsResult.StructuredContent!.Value.GetProperty("maxTipTapNodeCount").GetInt32());
        Assert.Equal(200000, limitsResult.StructuredContent!.Value.GetProperty("maxTipTapTextLength").GetInt32());

        var httpUploadResult = await mcpClient.CallToolAsync(NotesMcpToolNames.PrepareHttpUpload, new Dictionary<string, object?>());
        Assert.Equal("/api/mcp/uploads/markdown", httpUploadResult.StructuredContent!.Value.GetProperty("uploadUrl").GetString());
        Assert.Equal("POST", httpUploadResult.StructuredContent!.Value.GetProperty("method").GetString());
        Assert.Equal("multipart/form-data", httpUploadResult.StructuredContent!.Value.GetProperty("contentType").GetString());
        Assert.Equal(4194304, httpUploadResult.StructuredContent!.Value.GetProperty("maxUploadBytes").GetInt32());
        Assert.Contains(NotesMcpToolNames.CreatePage, httpUploadResult.StructuredContent!.Value.GetProperty("nextStep").GetString(), StringComparison.Ordinal);

        var guideResult = await mcpClient.ReadResourceAsync("notes://guide");
        var guide = Assert.IsType<TextResourceContents>(Assert.Single(guideResult.Contents));
        Assert.Contains(NotesMcpToolNames.PrepareHttpUpload, guide.Text, StringComparison.Ordinal);
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
    public async Task RenameItem_AcceptsResourceStylePagePrefixForBareStoredPath()
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

        var renameResult = await mcpClient.CallToolAsync(NotesMcpToolNames.RenameItem, new Dictionary<string, object?>
        {
            ["notebookSlug"] = "architecture-notes",
            ["path"] = "page/overview",
            ["title"] = "Renamed Overview"
        });

        Assert.False(renameResult.IsError ?? false);
        Assert.Equal("Renamed Overview", renameResult.StructuredContent!.Value.GetProperty("title").GetString());
    }

    [Fact]
    public async Task RenameItem_AcceptsBarePathForLegacyPagePrefixedStoredPath()
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

        var renameResult = await mcpClient.CallToolAsync(NotesMcpToolNames.RenameItem, new Dictionary<string, object?>
        {
            ["notebookSlug"] = "architecture-notes",
            ["path"] = "legacy-overview",
            ["title"] = "Renamed Legacy Overview"
        });

        Assert.False(renameResult.IsError ?? false);
        Assert.Equal("Renamed Legacy Overview", renameResult.StructuredContent!.Value.GetProperty("title").GetString());
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

    [Fact]
    public async Task PrecisionEditTools_CanMutatePageContent()
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

        var replaceResult = await mcpClient.CallToolAsync(NotesMcpToolNames.ReplaceBlockAtIndex, new Dictionary<string, object?>
        {
            ["notebookSlug"] = "architecture-notes",
            ["path"] = "overview",
            ["index"] = 0,
            ["block"] = JsonSerializer.SerializeToElement(new { type = "heading", attrs = new { level = 1 }, content = new[] { new { type = "text", text = "New heading" } } })
        });
        Assert.False(replaceResult.IsError ?? false);
        Assert.Equal(5, replaceResult.StructuredContent!.Value.GetProperty("tipTapNodeCount").GetInt32());

        var insertResult = await mcpClient.CallToolAsync(NotesMcpToolNames.InsertBlocksAtIndex, new Dictionary<string, object?>
        {
            ["notebookSlug"] = "architecture-notes",
            ["path"] = "overview",
            ["index"] = 1,
            ["blocks"] = JsonSerializer.SerializeToElement(new[]
            {
                new { type = "paragraph", content = new[] { new { type = "text", text = "Inserted paragraph." } } }
            })
        });
        Assert.False(insertResult.IsError ?? false);
        Assert.Equal(7, insertResult.StructuredContent!.Value.GetProperty("tipTapNodeCount").GetInt32());

        var deleteResult = await mcpClient.CallToolAsync(NotesMcpToolNames.DeleteBlockAtIndex, new Dictionary<string, object?>
        {
            ["notebookSlug"] = "architecture-notes",
            ["path"] = "overview",
            ["index"] = 1
        });
        Assert.False(deleteResult.IsError ?? false);
        Assert.Equal(3, deleteResult.StructuredContent!.Value.GetProperty("tipTapNodeCount").GetInt32());

        var replaceTextResult = await mcpClient.CallToolAsync(NotesMcpToolNames.ReplaceText, new Dictionary<string, object?>
        {
            ["notebookSlug"] = "architecture-notes",
            ["path"] = "overview",
            ["searchText"] = "First",
            ["replacementText"] = "Updated",
            ["replaceAll"] = false
        });
        Assert.False(replaceTextResult.IsError ?? false);
    }

    [Fact]
    public async Task CreatePage_RejectsTitleLongerThan160Characters()
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

        var createResult = await mcpClient.CallToolAsync(NotesMcpToolNames.CreatePage, new Dictionary<string, object?>
        {
            ["notebookSlug"] = "architecture-notes",
            ["title"] = new string('a', 161)
        });

        Assert.True(createResult.IsError ?? false);
        Assert.Equal("validation_error", createResult.StructuredContent!.Value.GetProperty("code").GetString());
    }
}
