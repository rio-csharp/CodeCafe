using CodeCafe.Application.Notes;
using CodeCafe.WebApi.Mcp;
using CodeCafe.WebApi.Tests.Auth;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
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
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.GetLimits);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.CreateUpload);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.AppendUploadChunk);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.DiscardUpload);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.RenameItem);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.Search);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.GetPage);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.AppendBlocksToPage);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.ArchiveItem);
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.RestoreItem);
        Assert.All(tools, tool => Assert.Matches("^[a-zA-Z0-9_-]{1,64}$", tool.Name));
        Assert.Contains(resources, resource => resource.Uri == "notes://guide");
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

        var guideResult = await mcpClient.ReadResourceAsync("notes://guide");
        var guideResource = Assert.IsType<TextResourceContents>(Assert.Single(guideResult.Contents));
        Assert.Contains(NotesMcpToolNames.CreateUpload, guideResource.Text);
        Assert.Contains("markdown", guideResource.Text, StringComparison.OrdinalIgnoreCase);
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
                ["contentJson"] = CreateDocJson("Contract draft")
            });
        var createdPath = created.StructuredContent!.Value.GetProperty("path").GetString();
        Assert.Equal("source/api-contracts", createdPath);

        var updated = await mcpClient.CallToolAsync(
            NotesMcpToolNames.UpdatePageContentJson,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = createdPath,
                ["contentJson"] = CreateDocJson("Updated draft")
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
                ["blocks"] = CreateBlocksJson("Appended block")
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
    public async Task McpWriteTools_SupportChunkedMarkdownAndJsonUploads()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true,
            McpMaxUploadChunkBytes = 1024
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"upload-import+{Guid.NewGuid():N}@example.com", "203.0.113.152");
        var notebook = await CreateNotebookAsync(client, "MCP Upload Notebook");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read", "notes.write");

        var markdownUploadId = await UploadTextAsync(
            mcpClient,
            "import.md",
            "text/markdown",
            """
            # Imported Draft

            Intro paragraph from markdown.

            - First bullet
            - Second bullet
            """,
            700);

        var created = await mcpClient.CallToolAsync(
            NotesMcpToolNames.CreatePage,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["title"] = "Imported Page",
                ["contentUploadId"] = markdownUploadId,
                ["contentFormat"] = "markdown"
            });
        Assert.False(created.IsError ?? false, ReadText(created));
        var createdPath = created.StructuredContent!.Value.GetProperty("path").GetString();

        var updatedJsonUploadId = await UploadTextAsync(
            mcpClient,
            "page.json",
            "application/json",
            CreateDocElement("Updated from uploaded json").GetRawText(),
            700);

        var updated = await mcpClient.CallToolAsync(
            NotesMcpToolNames.UpdatePageContentJson,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = createdPath,
                ["contentUploadId"] = updatedJsonUploadId,
                ["contentFormat"] = "tiptap_json"
            });

        Assert.False(updated.IsError ?? false, ReadText(updated));

        var appendMarkdownUploadId = await UploadTextAsync(
            mcpClient,
            "append.md",
            "text/markdown",
            """
            ## Appended Section

            More detail from markdown append.
            """,
            700);

        var appended = await mcpClient.CallToolAsync(
            NotesMcpToolNames.AppendBlocksToPage,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = createdPath,
                ["blocksUploadId"] = appendMarkdownUploadId,
                ["blocksFormat"] = "markdown"
            });

        Assert.False(appended.IsError ?? false, ReadText(appended));

        var pageAfterUpdate = await mcpClient.CallToolAsync(
            NotesMcpToolNames.GetPage,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = createdPath
            });
        var plainText = pageAfterUpdate.StructuredContent!.Value.GetProperty("plainTextContent").GetString();
        Assert.Contains("Updated from uploaded json", plainText, StringComparison.Ordinal);
        Assert.Contains("Appended Section", plainText, StringComparison.Ordinal);
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
                ["contentJson"] = CreateDocJson("Checklist draft")
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
                ["blocks"] = CreateDocJson("not-an-array")
            });

        AssertToolError(result, "invalid_blocks");
    }

    [Fact]
    public async Task McpGetLimits_ReturnsConfiguredThresholds()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true,
            McpMaxInlineContentBytes = 2048,
            McpMaxUploadChunkBytes = 4096,
            McpMaxUploadBytes = 8192,
            McpMaxPageContentBytes = 16384,
            McpMaxListItemsLimit = 55
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"limits+{Guid.NewGuid():N}@example.com", "203.0.113.151");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read", "notes.write");

        var result = await mcpClient.CallToolAsync(NotesMcpToolNames.GetLimits, new Dictionary<string, object?>());
        var content = result.StructuredContent!.Value;
        Assert.Equal(2048, content.GetProperty("maxInlineContentBytes").GetInt32());
        Assert.Equal(4096, content.GetProperty("maxUploadChunkBytes").GetInt32());
        Assert.Equal(8192, content.GetProperty("maxUploadBytes").GetInt32());
        Assert.Equal(16384, content.GetProperty("maxPageContentBytes").GetInt32());
        Assert.Equal(55, content.GetProperty("maxListItemsLimit").GetInt32());
    }

    [Fact]
    public async Task McpListItems_SupportsArchivedFilteringAndPagination()
    {
        using var factory = new AuthApiFactory { McpEnabled = true, McpMaxListItemsLimit = 2 };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"list-items+{Guid.NewGuid():N}@example.com", "203.0.113.154");
        var notebook = await CreateNotebookAsync(client, "List Items Notebook");
        await CreateFolderAsync(client, notebook.Id, "Folder A", 1);
        await CreateFolderAsync(client, notebook.Id, "Folder B", 2);
        await CreatePageAsync(client, notebook.Id, "Alpha", "Alpha body");
        var pageB = await CreatePageAsync(client, notebook.Id, "Beta", "Beta body");
        await ArchiveItemAsync(client, notebook.Id, pageB.Id);
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read", "notes.write");

        var activeOnly = await mcpClient.CallToolAsync(
            NotesMcpToolNames.ListItems,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["type"] = "page"
            });
        Assert.DoesNotContain(
            activeOnly.StructuredContent!.Value.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("path").GetString() == pageB.Path);

        var archivedIncluded = await mcpClient.CallToolAsync(
            NotesMcpToolNames.ListItems,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["type"] = "page",
                ["includeArchived"] = true,
                ["offset"] = 0,
                ["limit"] = 2
            });

        Assert.Equal(2, archivedIncluded.StructuredContent!.Value.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, archivedIncluded.StructuredContent!.Value.GetProperty("returnedCount").GetInt32());
    }

    [Fact]
    public async Task McpListItems_RejectsArchivedVisibilityForNonOwner()
    {
        using var factory = new AuthApiFactory { McpEnabled = true };
        using var owner = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        using var other = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(owner, $"archived-owner+{Guid.NewGuid():N}@example.com", "203.0.113.160");
        await RegisterAsync(other, $"archived-other+{Guid.NewGuid():N}@example.com", "203.0.113.161");

        var notebook = await CreateNotebookAsync(owner, "Archived Visibility Notebook");
        var page = await CreatePageAsync(owner, notebook.Id, "Archived Page", "Text");
        await ArchiveItemAsync(owner, notebook.Id, page.Id);

        await using var otherMcpClient = await McpTestAuth.CreateMcpClientAsync(factory, other, "notes.read");
        var result = await otherMcpClient.CallToolAsync(
            NotesMcpToolNames.ListItems,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["includeArchived"] = true
            });

        AssertToolError(result, "notebook_forbidden");
    }

    [Fact]
    public async Task McpUploadStore_ExpiresIdleUploads()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true,
            McpUploadIdleTimeoutSeconds = 1
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"upload-expire+{Guid.NewGuid():N}@example.com", "203.0.113.162");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read", "notes.write");

        var uploadId = await UploadTextAsync(mcpClient, "expire.md", "text/markdown", "hello", 16);
        await Task.Delay(TimeSpan.FromMilliseconds(1200));

        var result = await mcpClient.CallToolAsync(
            NotesMcpToolNames.AppendUploadChunk,
            new Dictionary<string, object?>
            {
                ["uploadId"] = uploadId,
                ["chunkText"] = "world"
            });

        AssertToolError(result, "upload_not_found");
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
    public async Task McpPrompt_WhenMissingReadScope_ThrowsProtocolError()
    {
        using var factory = new AuthApiFactory { McpEnabled = true };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"prompt-scope+{Guid.NewGuid():N}@example.com", "203.0.113.134");
        var notebook = await CreateNotebookAsync(client, "Prompt Scope Notebook");
        var page = await CreatePageAsync(client, notebook.Id, "Prompt Scope Page", "Prompt text");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.write");

        var exception = await Assert.ThrowsAsync<McpProtocolException>(async () =>
            await mcpClient.GetPromptAsync(
                "notes.summarize_page",
                new Dictionary<string, object?>
                {
                    ["notebookSlug"] = notebook.Slug,
                    ["path"] = page.Path
                }));

        Assert.Contains("insufficient_scope", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task McpResource_WhenItemMissing_ThrowsProtocolError()
    {
        using var factory = new AuthApiFactory { McpEnabled = true };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"resource-missing+{Guid.NewGuid():N}@example.com", "203.0.113.135");
        var notebook = await CreateNotebookAsync(client, "Resource Missing Notebook");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read");

        var exception = await Assert.ThrowsAsync<McpProtocolException>(async () =>
            await mcpClient.ReadResourceAsync($"page://{notebook.Slug}/missing-page"));

        Assert.Contains("notebook_item_not_found", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task McpCreateNotebook_RejectsInvalidVisibility()
    {
        using var factory = new AuthApiFactory { McpEnabled = true };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"visibility+{Guid.NewGuid():N}@example.com", "203.0.113.130");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.write");

        var result = await mcpClient.CallToolAsync(
            NotesMcpToolNames.CreateNotebook,
            new Dictionary<string, object?>
            {
                ["title"] = "Invalid Visibility",
                ["visibility"] = "secret"
            });

        AssertToolError(result, "invalid_visibility");
    }

    [Fact]
    public async Task McpArchiveItem_RejectsAlreadyArchivedItem()
    {
        using var factory = new AuthApiFactory { McpEnabled = true };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"archive-dup+{Guid.NewGuid():N}@example.com", "203.0.113.131");
        var notebook = await CreateNotebookAsync(client, "Archive Dup Notebook");
        var page = await CreatePageAsync(client, notebook.Id, "Archive Dup Page", "Text");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read", "notes.write");

        var first = await mcpClient.CallToolAsync(
            NotesMcpToolNames.ArchiveItem,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = page.Path
            });
        Assert.False(first.IsError ?? false);

        var second = await mcpClient.CallToolAsync(
            NotesMcpToolNames.ArchiveItem,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = page.Path
            });
        AssertToolError(second, "notebook_item_archived");
    }

    [Fact]
    public async Task McpDeleteItem_RejectsNonArchivedItem()
    {
        using var factory = new AuthApiFactory { McpEnabled = true };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"delete-live+{Guid.NewGuid():N}@example.com", "203.0.113.132");
        var notebook = await CreateNotebookAsync(client, "Delete Live Notebook");
        var page = await CreatePageAsync(client, notebook.Id, "Delete Live Page", "Text");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read", "notes.write");

        var result = await mcpClient.CallToolAsync(
            NotesMcpToolNames.DeleteItem,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = page.Path
            });
        AssertToolError(result, "notebook_item_not_archived");
        Assert.Equal(
            $"Call {NotesMcpToolNames.ArchiveItem} first, then retry {NotesMcpToolNames.DeleteItem}.",
            result.StructuredContent!.Value.GetProperty("suggestion").GetString());
    }

    [Fact]
    public async Task McpUpdatePageContentJson_RejectsOptimisticConcurrencyMismatch()
    {
        using var factory = new AuthApiFactory { McpEnabled = true };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"concurrency+{Guid.NewGuid():N}@example.com", "203.0.113.133");
        var notebook = await CreateNotebookAsync(client, "Concurrency Notebook");
        var page = await CreatePageAsync(client, notebook.Id, "Concurrency Page", "Original");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read", "notes.write");

        var result = await mcpClient.CallToolAsync(
            NotesMcpToolNames.UpdatePageContentJson,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = page.Path,
                ["contentJson"] = CreateDocJson("Updated"),
                ["expectedUpdatedAtUtc"] = DateTimeOffset.UtcNow.AddHours(-1).ToString("O")
            });
        AssertToolError(result, "content_conflict");
    }

    [Fact]
    public async Task McpUpdatePageContentJson_RejectsOversizedInlinePayloadWithSuggestion()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true,
            McpMaxInlineContentBytes = 128
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"payload-limit+{Guid.NewGuid():N}@example.com", "203.0.113.153");
        var notebook = await CreateNotebookAsync(client, "Payload Limit Notebook");
        var page = await CreatePageAsync(client, notebook.Id, "Blocked Page", "Original");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read", "notes.write");

        var oversized = CreateDocElement(new string('x', 512));
        var result = await mcpClient.CallToolAsync(
            NotesMcpToolNames.UpdatePageContentJson,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["path"] = page.Path,
                ["contentJson"] = oversized
            });

        AssertToolError(result, "content_too_large");
        Assert.Contains("exceeds the limit", result.StructuredContent!.Value.GetProperty("message").GetString(), StringComparison.Ordinal);
        Assert.Contains(NotesMcpToolNames.CreateUpload, result.StructuredContent!.Value.GetProperty("suggestion").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task McpWriteMutation_WhenAuditFails_RollsBackBusinessChange()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true,
            FailMcpAuditWrites = true
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"audit-rollback+{Guid.NewGuid():N}@example.com", "203.0.113.136");
        var notebook = await CreateNotebookAsync(client, "Audit Rollback Notebook");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read", "notes.write");

        var result = await mcpClient.CallToolAsync(
            NotesMcpToolNames.CreatePage,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug,
                ["title"] = "Should Roll Back",
                ["contentJson"] = CreateDocJson("Never persisted")
            });

        Assert.True(result.IsError ?? false);

        var listResult = await mcpClient.CallToolAsync(
            NotesMcpToolNames.ListItems,
            new Dictionary<string, object?>
            {
                ["notebookSlug"] = notebook.Slug
            });

        Assert.DoesNotContain(
            listResult.StructuredContent!.Value.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("title").GetString() == "Should Roll Back");
    }

    [Fact]
    public async Task McpWriteMutation_WhenFailureAuditFails_StillReturnsValidationError()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true,
            FailMcpAuditWrites = true
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"audit-validation+{Guid.NewGuid():N}@example.com", "203.0.113.137");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.write");

        var result = await mcpClient.CallToolAsync(
            NotesMcpToolNames.CreateNotebook,
            new Dictionary<string, object?>
            {
                ["title"] = "Invalid Visibility",
                ["visibility"] = "secret"
            });

        AssertToolError(result, "invalid_visibility");
    }

    [Fact]
    public async Task McpUploadTools_WhenAuditFails_StillReturnOriginalToolResults()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true,
            FailMcpAuditWrites = true
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await RegisterAsync(client, $"upload-audit+{Guid.NewGuid():N}@example.com", "203.0.113.163");
        await using var mcpClient = await McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read", "notes.write");

        var createResult = await mcpClient.CallToolAsync(
            NotesMcpToolNames.CreateUpload,
            new Dictionary<string, object?>
            {
                ["fileName"] = "audit.md",
                ["mediaType"] = "text/markdown"
            });

        Assert.False(createResult.IsError ?? false, ReadText(createResult));
        var uploadId = createResult.StructuredContent!.Value.GetProperty("uploadId").GetString();

        var appendResult = await mcpClient.CallToolAsync(
            NotesMcpToolNames.AppendUploadChunk,
            new Dictionary<string, object?>
            {
                ["uploadId"] = uploadId,
                ["chunkText"] = "# hello"
            });

        Assert.False(appendResult.IsError ?? false, ReadText(appendResult));

        var discardResult = await mcpClient.CallToolAsync(
            NotesMcpToolNames.DiscardUpload,
            new Dictionary<string, object?>
            {
                ["uploadId"] = uploadId
            });

        Assert.False(discardResult.IsError ?? false, ReadText(discardResult));
    }

    [Fact]
    public async Task McpMutationExecutor_WhenMutationFails_DoesNotPersistTrackedChanges()
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

        await RegisterAsync(client, $"mutation-consistency+{Guid.NewGuid():N}@example.com", "203.0.113.138");
        var currentUser = await GetCurrentUserAsync(client);
        var notebook = await CreateNotebookAsync(client, "Mutation Consistency Notebook");
        var page = await CreatePageAsync(client, notebook.Id, "Original Title", "Original text");

        using (var scope = factory.Services.CreateScope())
        {
            var executor = scope.ServiceProvider.GetRequiredService<IMcpMutationExecutor>();
            var notebookCommandService = scope.ServiceProvider.GetRequiredService<INotebookCommandService>();
            var principal = CreatePrincipal(currentUser.Id);

            var result = await executor.ExecuteAsync(
                principal,
                NotesMcpToolNames.UpdatePageContentJson,
                async ct =>
                {
                    var updateResult = await notebookCommandService.UpdateNotebookItemAsync(
                        notebook.Id,
                        page.Id,
                        currentUser.Id,
                        "Renamed Title",
                        default,
                        null,
                        JsonSerializer.SerializeToElement("invalid"),
                        ct);

                    return updateResult.Succeeded
                        ? McpMutationResult<TestToolResponse>.Success(
                            new TestToolResponse("unexpected"),
                            "unexpected",
                            notebook.Id,
                            page.Id)
                        : McpMutationResult<TestToolResponse>.Failure(
                            updateResult.Error!,
                            notebook.Id,
                            page.Id);
                },
                CancellationToken.None);

            AssertToolError(result, "invalid_tiptap_document");
        }

        using (var verificationScope = factory.Services.CreateScope())
        {
            var notebookQueryService = verificationScope.ServiceProvider.GetRequiredService<INotebookQueryService>();
            var itemsResult = await notebookQueryService.GetNotebookItemsAsync(
                notebook.Id,
                currentUser.Id,
                search: null,
                CancellationToken.None);

            Assert.True(itemsResult.Succeeded);
            var item = Assert.Single(itemsResult.Value!);
            Assert.Equal("Original Title", item.Title);
            Assert.Equal(page.Path, item.Path);
        }
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

    private static JsonElement CreateDocJson(string text) => CreateDocElement(text);

    private static JsonElement CreateBlocksJson(string text)
    {
        return JsonSerializer.SerializeToElement(new object[]
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

    private static async Task ArchiveItemAsync(HttpClient client, Guid notebookId, Guid itemId)
    {
        var csrf = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/notes/{notebookId}/items/{itemId}/archive");
        request.Headers.Add("X-CSRF-TOKEN", csrf);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<string> UploadTextAsync(
        McpClient mcpClient,
        string fileName,
        string mediaType,
        string content,
        int chunkSize)
    {
        var createResult = await mcpClient.CallToolAsync(
            NotesMcpToolNames.CreateUpload,
            new Dictionary<string, object?>
            {
                ["fileName"] = fileName,
                ["mediaType"] = mediaType
            });

        var uploadId = createResult.StructuredContent!.Value.GetProperty("uploadId").GetString()
            ?? throw new InvalidOperationException("Missing upload id.");

        for (var offset = 0; offset < content.Length; offset += chunkSize)
        {
            var chunk = content.Substring(offset, Math.Min(chunkSize, content.Length - offset));
            var appendResult = await mcpClient.CallToolAsync(
                NotesMcpToolNames.AppendUploadChunk,
                new Dictionary<string, object?>
                {
                    ["uploadId"] = uploadId,
                    ["chunkText"] = chunk
                });

            Assert.False(appendResult.IsError ?? false, ReadText(appendResult));
        }

        return uploadId;
    }

    private static string ReadText(CallToolResult result)
        => string.Join("\n", result.Content.OfType<TextContentBlock>().Select(block => block.Text));

    private static async Task<TestCurrentUser> GetCurrentUserAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/auth/me");
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var user = json.RootElement.GetProperty("user");
        return new TestCurrentUser(user.GetProperty("id").GetGuid());
    }

    private static ClaimsPrincipal CreatePrincipal(Guid userId)
        => new(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            },
            authenticationType: "Test"));

    private static void AssertToolError(CallToolResult result, string code)
    {
        Assert.True(result.IsError);
        Assert.Equal(code, result.StructuredContent!.Value.GetProperty("code").GetString());
    }

    private sealed record TestCurrentUser(Guid Id);

    private sealed record TestToolResponse(string Value);
}
