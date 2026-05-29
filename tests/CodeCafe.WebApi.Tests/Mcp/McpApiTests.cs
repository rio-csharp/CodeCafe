using CodeCafe.WebApi.Mcp;
using CodeCafe.WebApi.Tests.Auth;
using ModelContextProtocol.Protocol;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Text.Json;

namespace CodeCafe.WebApi.Tests.Mcp;

public sealed class McpApiTests
{
    [Fact]
    public async Task McpDiscovery_ListsNotesToolsResourcesAndPrompts()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"discover+{Guid.NewGuid():N}@example.com", "203.0.113.120");

        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read", "notes.write");

        var tools = await mcpClient.ListToolsAsync();
        var resources = await mcpClient.ListResourcesAsync();
        var resourceTemplates = await mcpClient.ListResourceTemplatesAsync();
        var prompts = await mcpClient.ListPromptsAsync();

        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.ListNotebooks);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.CreateNotebook);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.UpdateNotebook);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.DeleteNotebook);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.CreateFolder);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.RenameItem);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.Search);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.GetPage);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.AppendBlocksToPage);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.ArchiveItem);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.RestoreItem);
        Assert.All(tools, tool => Assert.Matches("^[a-zA-Z0-9_-]{1,64}$", tool.Name));
        Assert.Contains(resources, resource => resource.Uri == "notebooks://mine");
        Assert.Contains(resources, resource => resource.Uri == "notebooks://public");
        Assert.Contains(resourceTemplates, resource => resource.UriTemplate == "notebook://{slug}");
        Assert.Contains(resourceTemplates, resource => resource.UriTemplate == "notebook://{slug}/items");
        Assert.Contains(resourceTemplates, resource => resource.UriTemplate == "page://{slug}/{path}");
        Assert.Contains(prompts, prompt => prompt.Name == "notes.summarize_page");
        Assert.Contains(prompts, prompt => prompt.Name == "notes.review_for_staleness");
    }

    [Fact]
    public async Task McpReadToolsAndResources_ReturnNotebookAndPageData()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"read+{Guid.NewGuid():N}@example.com", "203.0.113.121");
        var notebook = await CreateNotebookAsync(client, "MCP Read Notebook");
        var page = await CreatePageAsync(client, notebook.Id, "Overview", "Initial text");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read");

        var notebookResult = await mcpClient.CallToolAsync(
            NotesMcpToolNames.GetNotebook,
            new Dictionary<string, object?> { ["slug"] = notebook.Slug });
        Assert.Equal("MCP Read Notebook", notebookResult.StructuredContent!.Value.GetProperty("title").GetString());
        Assert.Contains($"slug: {notebook.Slug}", ReadText(notebookResult));
        Assert.Contains($"notebookUri: notebook://{notebook.Slug}", ReadText(notebookResult));

        var listItemsResult = await mcpClient.CallToolAsync(
            NotesMcpToolNames.ListItems,
            new Dictionary<string, object?> { ["notebookSlug"] = notebook.Slug });
        Assert.Contains($"path: {page.Path}", ReadText(listItemsResult));
        Assert.Contains($"resourceUri: page://{notebook.Slug}/{page.Path}", ReadText(listItemsResult));

        var pageResult = await mcpClient.CallToolAsync(
            NotesMcpToolNames.GetPage,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = page.Path
            });
        Assert.Equal("Overview", pageResult.StructuredContent!.Value.GetProperty("title").GetString());
        Assert.Equal(JsonValueKind.String, pageResult.StructuredContent.Value.GetProperty("contentJson").ValueKind);
        Assert.Equal("Initial text", pageResult.StructuredContent!.Value.GetProperty("plainTextContent").GetString());
        Assert.Contains($"path: {page.Path}", ReadText(pageResult));
        Assert.Contains($"pageUri: page://{notebook.Slug}/{page.Path}", ReadText(pageResult));
        Assert.Contains("Plain text content:", ReadText(pageResult));
        Assert.Contains("TipTap JSON:", ReadText(pageResult));

        var discoveryResult = await mcpClient.ReadResourceAsync("notebooks://mine");
        var discoveryTextResource = Assert.IsType<TextResourceContents>(Assert.Single(discoveryResult.Contents));
        using var discoveryJson = JsonDocument.Parse(discoveryTextResource.Text);
        Assert.Contains(
            discoveryJson.RootElement.GetProperty("notebooks").EnumerateArray(),
            item => item.GetProperty("slug").GetString() == notebook.Slug
                && item.GetProperty("notebookUri").GetString() == $"notebook://{notebook.Slug}"
                && item.GetProperty("itemsUri").GetString() == $"notebook://{notebook.Slug}/items");

        var resourceResult = await mcpClient.ReadResourceAsync($"page://{notebook.Slug}/{page.Path}");
        var textResource = Assert.IsType<TextResourceContents>(Assert.Single(resourceResult.Contents));
        using var resourceJson = JsonDocument.Parse(textResource.Text);
        Assert.Equal(page.Path, resourceJson.RootElement.GetProperty("path").GetString());
    }

    [Fact]
    public async Task McpWriteTools_CreateUpdateAppendMoveReorderAndDeleteItems()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"write+{Guid.NewGuid():N}@example.com", "203.0.113.122");
        var notebook = await CreateNotebookAsync(client, "MCP Write Notebook");
        var sourceFolder = await CreateFolderAsync(client, notebook.Id, "Source", 1);
        var targetFolder = await CreateFolderAsync(client, notebook.Id, "Target", 2);
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read", "notes.write");

        var created = await mcpClient.CallToolAsync(
            NotesMcpToolNames.CreatePage,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["title"] = "API Contracts",
                ["parentPath"] = "source",
                ["sortOrder"] = 3,
                ["contentJson"] = CreateDocJsonString("Contract draft")
            });
        var createdPath = created.StructuredContent!.Value.GetProperty("path").GetString();
        Assert.Equal("source/api-contracts", createdPath);

        var updated = await mcpClient.CallToolAsync(
            NotesMcpToolNames.UpdatePageContentJson,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = createdPath,
                ["contentJson"] = CreateDocJsonString("Updated draft")
            });
        Assert.False(
            updated.IsError ?? false,
            string.Join(" | ", updated.Content.OfType<TextContentBlock>().Select(block => block.Text)));

        var pageAfterUpdate = await mcpClient.CallToolAsync(
            NotesMcpToolNames.GetPage,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = createdPath
            });
        Assert.Equal("Updated draft", pageAfterUpdate.StructuredContent!.Value.GetProperty("plainTextContent").GetString());

        var appended = await mcpClient.CallToolAsync(
            NotesMcpToolNames.AppendBlocksToPage,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = createdPath,
                ["blocks"] = CreateBlocksJsonString("Appended block")
            });
        Assert.NotEqual(true, appended.IsError);

        var pageAfterAppend = await mcpClient.CallToolAsync(
            NotesMcpToolNames.GetPage,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = createdPath
            });
        Assert.Contains("Appended block", pageAfterAppend.StructuredContent!.Value.GetProperty("plainTextContent").GetString());

        var moved = await mcpClient.CallToolAsync(
            NotesMcpToolNames.MoveItem,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = createdPath,
                ["targetParentPath"] = "target",
                ["sortOrder"] = 7
            });
        var movedPath = moved.StructuredContent!.Value.GetProperty("path").GetString();
        Assert.Equal("target/api-contracts", movedPath);

        var reordered = await mcpClient.CallToolAsync(
            NotesMcpToolNames.ReorderItems,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["items"] = new object[]
                {
                    new
                    {
                        path = movedPath,
                        parentPath = "target",
                        sortOrder = 1
                    }
                }
            });
        Assert.Contains(
            reordered.StructuredContent!.Value.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("path").GetString() == movedPath
                && item.GetProperty("sortOrder").GetInt32() == 1);

        var archived = await mcpClient.CallToolAsync(
            NotesMcpToolNames.ArchiveItem,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = movedPath
            });
        Assert.Equal(movedPath, archived.StructuredContent!.Value.GetProperty("path").GetString());

        var restored = await mcpClient.CallToolAsync(
            NotesMcpToolNames.RestoreItem,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = movedPath
            });
        Assert.Equal(movedPath, restored.StructuredContent!.Value.GetProperty("path").GetString());

        var archivedAgain = await mcpClient.CallToolAsync(
            NotesMcpToolNames.ArchiveItem,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = movedPath
            });
        Assert.Equal(movedPath, archivedAgain.StructuredContent!.Value.GetProperty("path").GetString());

        var deleted = await mcpClient.CallToolAsync(
            NotesMcpToolNames.DeleteItem,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = movedPath
            });
        Assert.Equal("deleted", deleted.StructuredContent!.Value.GetProperty("result").GetString());
    }

    [Fact]
    public async Task McpSearch_FindsItemMatchesOutsideNotebookMetadataMatches()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"search+{Guid.NewGuid():N}@example.com", "203.0.113.129");
        var notebook = await CreateNotebookAsync(client, "Alpha Project");
        var page = await CreatePageAsync(client, notebook.Id, "Research Notes", "Blue banana launch checklist");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read");

        var result = await mcpClient.CallToolAsync(
            NotesMcpToolNames.Search,
            new Dictionary<string, object?>
            {
                ["query"] = "banana",
                ["scope"] = "items"
            });

        Assert.False(
            result.IsError ?? false,
            string.Join(" | ", result.Content.OfType<TextContentBlock>().Select(block => block.Text)));
        Assert.Contains(
            result.StructuredContent!.Value.GetProperty("results").EnumerateArray(),
            item => item.GetProperty("path").GetString() == page.Path
                && item.GetProperty("notebookSlug").GetString() == notebook.Slug);
        Assert.Contains($"path: {page.Path}", ReadText(result));
        Assert.Contains($"resourceUri: page://{notebook.Slug}/{page.Path}", ReadText(result));
    }

    [Fact]
    public async Task McpNotebookTools_ListCreateUpdateRenameAndDeleteNotebook()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"notebook-tools+{Guid.NewGuid():N}@example.com", "203.0.113.124");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read", "notes.write");

        var createdNotebook = await mcpClient.CallToolAsync(
            NotesMcpToolNames.CreateNotebook,
            new Dictionary<string, object?>
            {
                ["title"] = "Ship Plan",
                ["description"] = "Initial draft",
                ["visibility"] = "private"
            });
        var notebookSlug = createdNotebook.StructuredContent!.Value.GetProperty("slug").GetString();
        Assert.Equal("Ship Plan", createdNotebook.StructuredContent!.Value.GetProperty("title").GetString());
        Assert.Equal("private", createdNotebook.StructuredContent!.Value.GetProperty("visibility").GetString());

        var listedNotebook = await mcpClient.CallToolAsync(
            NotesMcpToolNames.ListNotebooks,
            new Dictionary<string, object?>
            {
                ["scope"] = "mine"
            });
        Assert.Contains(
            listedNotebook.StructuredContent!.Value.GetProperty("notebooks").EnumerateArray(),
            notebook => notebook.GetProperty("slug").GetString() == notebookSlug);

        var createdFolder = await mcpClient.CallToolAsync(
            NotesMcpToolNames.CreateFolder,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebookSlug,
                ["title"] = "Drafts",
                ["sortOrder"] = 1
            });
        Assert.Equal("folder", createdFolder.StructuredContent!.Value.GetProperty("type").GetString());
        Assert.Equal("drafts", createdFolder.StructuredContent!.Value.GetProperty("path").GetString());

        var createdPage = await mcpClient.CallToolAsync(
            NotesMcpToolNames.CreatePage,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebookSlug,
                ["title"] = "Release Checklist",
                ["parentPath"] = "drafts",
                ["contentJson"] = CreateDocJsonString("Checklist draft")
            });
        var createdPagePath = createdPage.StructuredContent!.Value.GetProperty("path").GetString();
        Assert.Equal("drafts/release-checklist", createdPagePath);

        var renamedFolder = await mcpClient.CallToolAsync(
            NotesMcpToolNames.RenameItem,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebookSlug,
                ["path"] = "drafts",
                ["title"] = "Planning"
            });
        Assert.Equal("planning", renamedFolder.StructuredContent!.Value.GetProperty("path").GetString());

        var renamedPage = await mcpClient.CallToolAsync(
            NotesMcpToolNames.GetPage,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebookSlug,
                ["path"] = "planning/release-checklist"
            });
        Assert.Equal("Release Checklist", renamedPage.StructuredContent!.Value.GetProperty("title").GetString());

        var updatedNotebook = await mcpClient.CallToolAsync(
            NotesMcpToolNames.UpdateNotebook,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebookSlug,
                ["title"] = "Ship Plan Final",
                ["description"] = "",
                ["visibility"] = "public"
            });
        var updatedSlug = updatedNotebook.StructuredContent!.Value.GetProperty("slug").GetString();
        Assert.Equal("Ship Plan Final", updatedNotebook.StructuredContent!.Value.GetProperty("title").GetString());
        Assert.Equal("public", updatedNotebook.StructuredContent!.Value.GetProperty("visibility").GetString());
        Assert.True(updatedNotebook.StructuredContent!.Value.GetProperty("isPublished").GetBoolean());
        Assert.True(string.IsNullOrEmpty(updatedNotebook.StructuredContent!.Value.GetProperty("description").GetString()));
        Assert.NotEqual(notebookSlug, updatedSlug);

        var publicNotebooks = await mcpClient.CallToolAsync(
            NotesMcpToolNames.ListNotebooks,
            new Dictionary<string, object?>
            {
                ["scope"] = "public",
                ["query"] = "Ship Plan Final"
            });
        Assert.Contains(
            publicNotebooks.StructuredContent!.Value.GetProperty("notebooks").EnumerateArray(),
            notebook => notebook.GetProperty("slug").GetString() == updatedSlug);

        var deletedNotebook = await mcpClient.CallToolAsync(
            NotesMcpToolNames.DeleteNotebook,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = updatedSlug
            });
        Assert.Equal("deleted", deletedNotebook.StructuredContent!.Value.GetProperty("result").GetString());

        var missingNotebook = await mcpClient.CallToolAsync(
            NotesMcpToolNames.GetNotebook,
            new Dictionary<string, object?>
            {
                ["slug"] = updatedSlug
            });
        Assert.True(missingNotebook.IsError);
        Assert.Equal("notebook_not_found", missingNotebook.StructuredContent!.Value.GetProperty("code").GetString());
    }

    [Fact]
    public async Task McpWriteTools_RejectMissingWriteScope()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"scope+{Guid.NewGuid():N}@example.com", "203.0.113.125");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read");

        var result = await mcpClient.CallToolAsync(
            NotesMcpToolNames.CreateNotebook,
            new Dictionary<string, object?>
            {
                ["title"] = "Should Fail"
            });

        AssertToolError(result, "insufficient_scope");
    }

    [Fact]
    public async Task McpNotebookUpdate_RequiresAtLeastOneChange()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"update-validation+{Guid.NewGuid():N}@example.com", "203.0.113.126");
        var notebook = await CreateNotebookAsync(client, "No-Op Notebook");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read", "notes.write");

        var result = await mcpClient.CallToolAsync(
            NotesMcpToolNames.UpdateNotebook,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug
            });

        AssertToolError(result, "missing_changes");
    }

    [Fact]
    public async Task McpCreateFolder_RejectsPageParent()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"parent-validation+{Guid.NewGuid():N}@example.com", "203.0.113.127");
        var notebook = await CreateNotebookAsync(client, "Parent Validation Notebook");
        var page = await CreatePageAsync(client, notebook.Id, "Leaf Page", "Content");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read", "notes.write");

        var result = await mcpClient.CallToolAsync(
            NotesMcpToolNames.CreateFolder,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["title"] = "Should Fail",
                ["parentPath"] = page.Path
            });

        AssertToolError(result, "invalid_parent");
    }

    [Fact]
    public async Task McpAppendBlocks_RejectsNonArrayPayload()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"append-validation+{Guid.NewGuid():N}@example.com", "203.0.113.128");
        var notebook = await CreateNotebookAsync(client, "Append Validation Notebook");
        var page = await CreatePageAsync(client, notebook.Id, "Append Target", "Content");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read", "notes.write");

        var result = await mcpClient.CallToolAsync(
            NotesMcpToolNames.AppendBlocksToPage,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = page.Path,
                ["blocks"] = CreateDocJsonString("not-an-array")
            });

        AssertToolError(result, "invalid_blocks");
    }

    [Fact]
    public async Task McpPrompt_GetPromptReturnsMessages()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"prompt+{Guid.NewGuid():N}@example.com", "203.0.113.123");
        var notebook = await CreateNotebookAsync(client, "MCP Prompt Notebook");
        var page = await CreatePageAsync(client, notebook.Id, "Prompt Page", "Prompt text");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read");
        var promptResult = await mcpClient.GetPromptAsync(
            "notes.summarize_page",
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = page.Path
            });

        Assert.NotEmpty(promptResult.Messages);
    }

    [Fact]
    public async Task McpProtectedResourceMetadata_ReturnsConfiguredResourceInfo()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true
        };
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/.well-known/oauth-protected-resource/mcp");
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("/mcp", new Uri(json.RootElement.GetProperty("resource").GetString()!).AbsolutePath);
        Assert.Contains(
            json.RootElement.GetProperty("scopes_supported").EnumerateArray().Select(value => value.GetString()),
            value => value == "notes.read");
    }

    [Fact]
    public async Task OpenIdConnectDiscovery_WhenMcpEnabled_IsAnonymous()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true
        };
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/.well-known/openid-configuration");
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("https://codecafe.test/", json.RootElement.GetProperty("issuer").GetString());
        Assert.EndsWith("/connect/authorize", json.RootElement.GetProperty("authorization_endpoint").GetString(), StringComparison.Ordinal);
        Assert.EndsWith("/connect/token", json.RootElement.GetProperty("token_endpoint").GetString(), StringComparison.Ordinal);
    }

    private static async Task RegisterAsync(HttpClient client, string email, string clientIp)
    {
        var csrf = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(new
            {
                email,
                password = "Password123!",
                displayName = "Yao"
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        request.Headers.Add("X-Forwarded-For", clientIp);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/auth/csrf");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("token").GetString() ?? throw new InvalidOperationException("Missing CSRF token.");
    }

    private static async Task<(Guid Id, string Slug)> CreateNotebookAsync(HttpClient client, string title)
    {
        var csrf = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/notes")
        {
            Content = JsonContent.Create(new
            {
                title,
                visibility = "private"
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (
            json.RootElement.GetProperty("id").GetGuid(),
            json.RootElement.GetProperty("slug").GetString() ?? throw new InvalidOperationException("Missing slug."));
    }

    private static async Task<Guid> CreateFolderAsync(HttpClient client, Guid notebookId, string title, int sortOrder)
    {
        var csrf = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/notes/{notebookId}/items")
        {
            Content = JsonContent.Create(new
            {
                type = "folder",
                title,
                sortOrder
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<(Guid Id, string Path)> CreatePageAsync(HttpClient client, Guid notebookId, string title, string text)
    {
        var csrf = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/notes/{notebookId}/items")
        {
            Content = JsonContent.Create(new
            {
                type = "page",
                title,
                sortOrder = 1,
                contentJson = CreateDocElement(text)
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (
            json.RootElement.GetProperty("id").GetGuid(),
            json.RootElement.GetProperty("path").GetString() ?? throw new InvalidOperationException("Missing path."));
    }

    private static JsonElement CreateDocElement(string text)
    {
        return JsonSerializer.SerializeToElement(new
        {
            type = "doc",
            content = new object[]
            {
                new
                {
                    type = "paragraph",
                    content = new object[]
                    {
                        new { type = "text", text }
                    }
                }
            }
        });
    }

    private static string CreateDocJsonString(string text)
    {
        return CreateDocElement(text).GetRawText();
    }

    private static string CreateBlocksJsonString(string text)
    {
        return JsonSerializer.Serialize(new object[]
        {
            new
            {
                type = "paragraph",
                content = new object[]
                {
                    new { type = "text", text }
                }
            }
        });
    }

    private static string ReadText(CallToolResult result)
        => string.Join("\n", result.Content.OfType<TextContentBlock>().Select(block => block.Text));

    private static void AssertToolError(CallToolResult result, string code)
    {
        Assert.True(result.IsError);
        Assert.Equal(code, result.StructuredContent!.Value.GetProperty("code").GetString());
    }
}
