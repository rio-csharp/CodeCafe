using CodeCafe.WebApi.Tests.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CodeCafe.WebApi.Tests.Notes;

public sealed class NotesApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public NotesApiTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NotesFlow_CreatesPublicNotebookAndNestedPage()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"notes+{Guid.NewGuid():N}@example.com", "203.0.113.80");

        var createNotebook = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/notes", new
        {
            title = "Auth Notes",
            description = "Cookie and JWT notes",
            visibility = "public",
            isPublished = true
        });
        createNotebook.EnsureSuccessStatusCode();

        var notebook = await ReadJsonAsync(createNotebook);
        var notebookId = notebook.RootElement.GetProperty("id").GetGuid();
        var notebookSlug = notebook.RootElement.GetProperty("slug").GetString();

        var notebookBySlug = await client.GetAsync($"/api/notes/{notebookSlug}");
        notebookBySlug.EnsureSuccessStatusCode();

        var createFolder = await SendWithCsrfAsync(client, HttpMethod.Post, $"/api/notes/{notebookId}/items", new
        {
            type = "folder",
            title = "Auth",
            sortOrder = 1
        });
        createFolder.EnsureSuccessStatusCode();

        var folder = await ReadJsonAsync(createFolder);
        var folderId = folder.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("auth", folder.RootElement.GetProperty("path").GetString());

        var createPage = await SendWithCsrfAsync(client, HttpMethod.Post, $"/api/notes/{notebookId}/items", new
        {
            parentId = folderId,
            type = "page",
            title = "Cookie vs JWT",
            sortOrder = 1,
            contentJson = new
            {
                type = "doc",
                content = new object[]
                {
                    new { type = "paragraph" }
                }
            },
            plainTextContent = "Cookie vs JWT"
        });
        createPage.EnsureSuccessStatusCode();

        var page = await ReadJsonAsync(createPage);
        Assert.Equal("auth/cookie-vs-jwt", page.RootElement.GetProperty("path").GetString());
        Assert.Equal("tiptap_json", page.RootElement.GetProperty("contentFormat").GetString());

        using var publicClient = _factory.CreateClient();
        var publicNotebooks = await publicClient.GetAsync("/api/notes/public");
        publicNotebooks.EnsureSuccessStatusCode();
        var publicNotebookList = await ReadJsonAsync(publicNotebooks);
        var listedNotebook = publicNotebookList.RootElement.EnumerateArray().Single(value =>
            value.GetProperty("slug").GetString() == notebookSlug);
        Assert.Equal(2, listedNotebook.GetProperty("itemCount").GetInt32());
        Assert.Equal(1, listedNotebook.GetProperty("folderCount").GetInt32());
        Assert.Equal(1, listedNotebook.GetProperty("pageCount").GetInt32());
        Assert.Equal(0, listedNotebook.GetProperty("favoriteCount").GetInt32());
        Assert.False(listedNotebook.GetProperty("isFavoritedByMe").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, listedNotebook.GetProperty("lastActivityAtUtc").ValueKind);

        var publicPage = await publicClient.GetAsync($"/api/notes/public/{notebookSlug}/items/auth/cookie-vs-jwt");

        publicPage.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateNotebookItem_DerivesPlainTextFromContentJsonAndIgnoresClientPlainText()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"derived-text+{Guid.NewGuid():N}@example.com", "203.0.113.106");
        var notebookId = await CreateNotebookAsync(client, "Derived Text Notes", "private");

        var createPage = await SendWithCsrfAsync(client, HttpMethod.Post, $"/api/notes/{notebookId}/items", new
        {
            type = "page",
            title = "Search Source",
            sortOrder = 1,
            contentJson = CreateDoc("Server derived text"),
            plainTextContent = "Client supplied text"
        });
        createPage.EnsureSuccessStatusCode();

        var page = await ReadJsonAsync(createPage);
        Assert.Equal("Server derived text", page.RootElement.GetProperty("plainTextContent").GetString());
    }

    [Fact]
    public async Task CreateNotebookItem_StripsLeadingHeadingThatDuplicatesPageTitle()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"strip-title-create+{Guid.NewGuid():N}@example.com", "203.0.113.109");
        var notebookId = await CreateNotebookAsync(client, "Duplicate Title Notes", "private");

        var createPage = await SendWithCsrfAsync(client, HttpMethod.Post, $"/api/notes/{notebookId}/items", new
        {
            type = "page",
            title = "Authentication & Authorization",
            sortOrder = 1,
            contentJson = CreateDocWithLeadingHeading("Authentication & Authorization", "Body starts here")
        });
        createPage.EnsureSuccessStatusCode();

        var page = await ReadJsonAsync(createPage);
        Assert.Equal("Body starts here", page.RootElement.GetProperty("plainTextContent").GetString());

        var content = page.RootElement.GetProperty("contentJson").GetProperty("content").EnumerateArray().ToArray();
        Assert.Single(content);
        Assert.Equal("paragraph", content[0].GetProperty("type").GetString());
    }

    [Fact]
    public async Task CreateNotebookItem_InvalidTipTapDocument_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"invalid-content+{Guid.NewGuid():N}@example.com", "203.0.113.107");
        var notebookId = await CreateNotebookAsync(client, "Invalid Content Notes", "private");

        var createPage = await SendWithCsrfAsync(client, HttpMethod.Post, $"/api/notes/{notebookId}/items", new
        {
            type = "page",
            title = "Broken Page",
            sortOrder = 1,
            contentJson = new { type = "paragraph" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, createPage.StatusCode);
        var error = await ReadJsonAsync(createPage);
        Assert.Equal("invalid_tiptap_document", error.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task PublicEndpoints_DoNotReturnPrivateOrUnpublishedNotebooks()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"private+{Guid.NewGuid():N}@example.com", "203.0.113.81");

        var createNotebook = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/notes", new
        {
            title = "Private Notes",
            visibility = "private",
            isPublished = true
        });
        createNotebook.EnsureSuccessStatusCode();

        var notebook = await ReadJsonAsync(createNotebook);
        var slug = notebook.RootElement.GetProperty("slug").GetString();

        using var publicClient = _factory.CreateClient();
        var response = await publicClient.GetAsync($"/api/notes/public/{slug}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var privateSlugResponse = await publicClient.GetAsync($"/api/notes/{slug}");

        Assert.Equal(HttpStatusCode.Forbidden, privateSlugResponse.StatusCode);
    }

    [Fact]
    public async Task WriteEndpoints_RejectNonOwners()
    {
        using var owner = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(owner, $"owner+{Guid.NewGuid():N}@example.com", "203.0.113.82");

        var createNotebook = await SendWithCsrfAsync(owner, HttpMethod.Post, "/api/notes", new
        {
            title = "Owner Notes",
            visibility = "public",
            isPublished = true
        });
        createNotebook.EnsureSuccessStatusCode();
        var notebook = await ReadJsonAsync(createNotebook);
        var notebookId = notebook.RootElement.GetProperty("id").GetGuid();

        using var other = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(other, $"other+{Guid.NewGuid():N}@example.com", "203.0.113.83");

        var update = await SendWithCsrfAsync(other, HttpMethod.Put, $"/api/notes/{notebookId}", new
        {
            title = "Taken Over",
            visibility = "public",
            isPublished = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
    }

    [Fact]
    public async Task UnlistedNotebook_IsReadableBySlugButNotListedPublicly()
    {
        using var owner = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(owner, $"unlisted+{Guid.NewGuid():N}@example.com", "203.0.113.84");

        var createNotebook = await SendWithCsrfAsync(owner, HttpMethod.Post, "/api/notes", new
        {
            title = "Hidden Notes",
            visibility = "unlisted"
        });
        createNotebook.EnsureSuccessStatusCode();
        var notebook = await ReadJsonAsync(createNotebook);
        var notebookId = notebook.RootElement.GetProperty("id").GetGuid();
        var slug = notebook.RootElement.GetProperty("slug").GetString();

        var createPage = await SendWithCsrfAsync(owner, HttpMethod.Post, $"/api/notes/{notebookId}/items", new
        {
            type = "page",
            title = "Secret Page",
            sortOrder = 1,
            plainTextContent = "Top secret"
        });
        createPage.EnsureSuccessStatusCode();

        using var anonymous = _factory.CreateClient();
        var listed = await anonymous.GetAsync("/api/notes/public");
        listed.EnsureSuccessStatusCode();
        var listedJson = await ReadJsonAsync(listed);
        Assert.DoesNotContain(listedJson.RootElement.EnumerateArray(), value =>
            value.GetProperty("slug").GetString() == slug);

        var detail = await anonymous.GetAsync($"/api/notes/{slug}");
        detail.EnsureSuccessStatusCode();

        var items = await anonymous.GetAsync($"/api/notes/{notebookId}/items");
        items.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task UpdateNotebookItem_CanMovePageIntoAnotherFolder()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"move+{Guid.NewGuid():N}@example.com", "203.0.113.85");
        var notebookId = await CreateNotebookAsync(client, "Move Notes", "private");

        var folderAId = await CreateFolderAsync(client, notebookId, "Backend", 1);
        var folderBId = await CreateFolderAsync(client, notebookId, "Frontend", 2);
        var page = await CreatePageAsync(client, notebookId, folderAId, "Auth Flow", 1, "Auth flow");
        var pageId = page.RootElement.GetProperty("id").GetGuid();

        var update = await SendWithCsrfAsync(client, HttpMethod.Put, $"/api/notes/{notebookId}/items/{pageId}", new
        {
            title = "Auth Flow",
            parentId = folderBId,
            sortOrder = 5,
            contentJson = new { type = "doc" },
            plainTextContent = "Auth flow"
        });
        update.EnsureSuccessStatusCode();

        var updatedPage = await ReadJsonAsync(update);
        Assert.Equal("frontend/auth-flow", updatedPage.RootElement.GetProperty("path").GetString());
        Assert.Equal(folderBId, updatedPage.RootElement.GetProperty("parentId").GetGuid());
    }

    [Fact]
    public async Task UpdateNotebookItem_RenamePreservesExistingSortOrderWhenSortOrderIsOmitted()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"rename-preserve-sort+{Guid.NewGuid():N}@example.com", "203.0.113.103");
        var notebookId = await CreateNotebookAsync(client, "Rename Notes", "private");

        var page = await CreatePageAsync(client, notebookId, null, "Introduction", 25, "Original content");
        var pageId = page.RootElement.GetProperty("id").GetGuid();

        var update = await SendWithCsrfAsync(client, HttpMethod.Put, $"/api/notes/{notebookId}/items/{pageId}", new
        {
            title = "Introduction Updated"
        });
        update.EnsureSuccessStatusCode();

        var updatedPage = await ReadJsonAsync(update);
        Assert.Equal(25, updatedPage.RootElement.GetProperty("sortOrder").GetInt32());
        Assert.Equal("introduction-updated", updatedPage.RootElement.GetProperty("path").GetString());
    }

    [Fact]
    public async Task UpdateNotebookItem_OmittedParentId_KeepsExistingParent()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"save-page+{Guid.NewGuid():N}@example.com", "203.0.113.96");
        var notebookId = await CreateNotebookAsync(client, "Save Notes", "private");

        var folderId = await CreateFolderAsync(client, notebookId, "Guides", 1);
        var page = await CreatePageAsync(client, notebookId, folderId, "Introduction", 1, "Initial");
        var pageId = page.RootElement.GetProperty("id").GetGuid();

        var update = await SendWithCsrfAsync(client, HttpMethod.Put, $"/api/notes/{notebookId}/items/{pageId}", new
        {
            title = "Introduction",
            sortOrder = 1,
            contentJson = new { type = "doc" },
            plainTextContent = "Updated content"
        });
        update.EnsureSuccessStatusCode();

        var updatedPage = await ReadJsonAsync(update);
        Assert.Equal(folderId, updatedPage.RootElement.GetProperty("parentId").GetGuid());
        Assert.Equal("guides/introduction", updatedPage.RootElement.GetProperty("path").GetString());
    }

    [Fact]
    public async Task UpdateNotebookItem_ExplicitNullParentId_MovesItemToRoot()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"move-root+{Guid.NewGuid():N}@example.com", "203.0.113.97");
        var notebookId = await CreateNotebookAsync(client, "Root Move Notes", "private");

        var folderId = await CreateFolderAsync(client, notebookId, "Guides", 1);
        var page = await CreatePageAsync(client, notebookId, folderId, "Introduction", 1, "Initial");
        var pageId = page.RootElement.GetProperty("id").GetGuid();

        var update = await SendWithCsrfAsync(client, HttpMethod.Put, $"/api/notes/{notebookId}/items/{pageId}", new
        {
            title = "Introduction",
            parentId = (Guid?)null,
            sortOrder = 1,
            contentJson = new { type = "doc" },
            plainTextContent = "Moved to root"
        });
        update.EnsureSuccessStatusCode();

        var updatedPage = await ReadJsonAsync(update);
        Assert.Equal(JsonValueKind.Null, updatedPage.RootElement.GetProperty("parentId").ValueKind);
        Assert.Equal("introduction", updatedPage.RootElement.GetProperty("path").GetString());
    }

    [Fact]
    public async Task UpdateNotebookItem_ContentUpdateDoesNotRequireSortOrder()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"content-no-sort+{Guid.NewGuid():N}@example.com", "203.0.113.104");
        var notebookId = await CreateNotebookAsync(client, "Content Notes", "private");

        var page = await CreatePageAsync(client, notebookId, null, "Introduction", 30, "Original content");
        var pageId = page.RootElement.GetProperty("id").GetGuid();

        var update = await SendWithCsrfAsync(client, HttpMethod.Put, $"/api/notes/{notebookId}/items/{pageId}", new
        {
            title = "Introduction",
            contentJson = new
            {
                type = "doc",
                content = new object[]
                {
                    new
                    {
                        type = "paragraph",
                        content = new object[]
                        {
                            new { type = "text", text = "Updated content" }
                        }
                    }
                }
            }
        });
        update.EnsureSuccessStatusCode();

        var updatedPage = await ReadJsonAsync(update);
        Assert.Equal(30, updatedPage.RootElement.GetProperty("sortOrder").GetInt32());
        Assert.Equal("Updated content", updatedPage.RootElement.GetProperty("plainTextContent").GetString());
    }

    [Fact]
    public async Task UpdateNotebookItem_InvalidTipTapDocument_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"invalid-update-content+{Guid.NewGuid():N}@example.com", "203.0.113.108");
        var notebookId = await CreateNotebookAsync(client, "Invalid Update Content Notes", "private");

        var page = await CreatePageAsync(client, notebookId, null, "Introduction", 30, "Original content");
        var pageId = page.RootElement.GetProperty("id").GetGuid();

        var update = await SendWithCsrfAsync(client, HttpMethod.Put, $"/api/notes/{notebookId}/items/{pageId}", new
        {
            title = "Introduction",
            contentJson = new
            {
                type = "doc",
                content = new object[]
                {
                    "not-a-node"
                }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        var error = await ReadJsonAsync(update);
        Assert.Equal("invalid_tiptap_document", error.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task UpdateNotebookItem_StripsLeadingHeadingThatDuplicatesPageTitle()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"strip-title-update+{Guid.NewGuid():N}@example.com", "203.0.113.110");
        var notebookId = await CreateNotebookAsync(client, "Update Duplicate Title Notes", "private");

        var page = await CreatePageAsync(client, notebookId, null, "Authentication & Authorization", 1, "Original content");
        var pageId = page.RootElement.GetProperty("id").GetGuid();

        var update = await SendWithCsrfAsync(client, HttpMethod.Put, $"/api/notes/{notebookId}/items/{pageId}", new
        {
            title = "Authentication & Authorization",
            contentJson = CreateDocWithLeadingHeading("Authentication & Authorization", "Updated body content")
        });
        update.EnsureSuccessStatusCode();

        var updatedPage = await ReadJsonAsync(update);
        Assert.Equal("Updated body content", updatedPage.RootElement.GetProperty("plainTextContent").GetString());

        var content = updatedPage.RootElement.GetProperty("contentJson").GetProperty("content").EnumerateArray().ToArray();
        Assert.Single(content);
        Assert.Equal("paragraph", content[0].GetProperty("type").GetString());
    }

    [Fact]
    public async Task UpdateNotebookItem_OmittedContentJson_KeepsExistingContent()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"keep-content+{Guid.NewGuid():N}@example.com", "203.0.113.101");
        var notebookId = await CreateNotebookAsync(client, "Keep Content Notes", "private");

        var page = await CreatePageAsync(client, notebookId, null, "Introduction", 1, "Original content");
        var pageId = page.RootElement.GetProperty("id").GetGuid();

        var update = await SendWithCsrfAsync(client, HttpMethod.Put, $"/api/notes/{notebookId}/items/{pageId}", new
        {
            title = "Introduction Updated",
            sortOrder = 2,
            plainTextContent = "Should be ignored"
        });
        update.EnsureSuccessStatusCode();

        var updatedPage = await ReadJsonAsync(update);
        Assert.Equal("Original content", updatedPage.RootElement.GetProperty("plainTextContent").GetString());
        Assert.Equal(JsonValueKind.Object, updatedPage.RootElement.GetProperty("contentJson").ValueKind);
    }

    [Fact]
    public async Task UpdateNotebookItem_ExplicitNullContentJson_ClearsExistingContent()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"clear-content+{Guid.NewGuid():N}@example.com", "203.0.113.102");
        var notebookId = await CreateNotebookAsync(client, "Clear Content Notes", "private");

        var page = await CreatePageAsync(client, notebookId, null, "Introduction", 1, "Original content");
        var pageId = page.RootElement.GetProperty("id").GetGuid();

        var update = await SendWithCsrfAsync(client, HttpMethod.Put, $"/api/notes/{notebookId}/items/{pageId}", new
        {
            title = "Introduction",
            sortOrder = 1,
            contentJson = (object?)null
        });
        update.EnsureSuccessStatusCode();

        var updatedPage = await ReadJsonAsync(update);
        Assert.Equal(JsonValueKind.Null, updatedPage.RootElement.GetProperty("contentJson").ValueKind);
        Assert.Equal(JsonValueKind.Null, updatedPage.RootElement.GetProperty("plainTextContent").ValueKind);
    }

    [Fact]
    public async Task UpdateNotebookItem_RenamingFolderUpdatesDescendantPaths()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"rename-folder+{Guid.NewGuid():N}@example.com", "203.0.113.105");
        var notebookId = await CreateNotebookAsync(client, "Nested Notes", "private");

        var folderId = await CreateFolderAsync(client, notebookId, "Guides", 1);
        await CreatePageAsync(client, notebookId, folderId, "Introduction", 1, "Original content");

        var update = await SendWithCsrfAsync(client, HttpMethod.Put, $"/api/notes/{notebookId}/items/{folderId}", new
        {
            title = "Tutorials"
        });
        update.EnsureSuccessStatusCode();

        var itemsResponse = await client.GetAsync($"/api/notes/{notebookId}/items");
        itemsResponse.EnsureSuccessStatusCode();
        var itemsJson = await ReadJsonAsync(itemsResponse);
        Assert.Contains(itemsJson.RootElement.EnumerateArray(), value =>
            value.GetProperty("path").GetString() == "tutorials/introduction");
    }

    [Fact]
    public async Task ReorderNotebookItems_CanMoveFolderAndDescendantPathsFollow()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"reorder+{Guid.NewGuid():N}@example.com", "203.0.113.86");
        var notebookId = await CreateNotebookAsync(client, "Reorder Notes", "private");

        var sourceFolderId = await CreateFolderAsync(client, notebookId, "Source", 1);
        var targetFolderId = await CreateFolderAsync(client, notebookId, "Target", 2);
        var childFolderId = await CreateFolderAsync(client, notebookId, "Child", 1, sourceFolderId);
        var childPage = await CreatePageAsync(client, notebookId, childFolderId, "Nested Page", 1, "Nested");

        var reorder = await SendWithCsrfAsync(client, HttpMethod.Put, $"/api/notes/{notebookId}/items/reorder", new
        {
            items = new object[]
            {
                new
                {
                    itemId = sourceFolderId,
                    parentId = targetFolderId,
                    sortOrder = 10
                }
            }
        });
        reorder.EnsureSuccessStatusCode();

        var itemsResponse = await ReadJsonAsync(reorder);
        Assert.Contains(itemsResponse.RootElement.GetProperty("items").EnumerateArray(), value =>
            value.GetProperty("id").GetGuid() == childPage.RootElement.GetProperty("id").GetGuid()
            && value.GetProperty("path").GetString() == "target/source/child/nested-page");
    }

    [Fact]
    public async Task NotebookEndpoints_SearchByTitleAndContent()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"search+{Guid.NewGuid():N}@example.com", "203.0.113.87");
        var notebookId = await CreateNotebookAsync(client, "Distributed Systems", "public", "Consensus notes");
        await CreatePageAsync(client, notebookId, null, "Raft Intro", 1, "leader election");

        using var anonymous = _factory.CreateClient();
        var publicSearch = await anonymous.GetAsync("/api/notes/public?search=Distributed");
        publicSearch.EnsureSuccessStatusCode();
        var publicSearchJson = await ReadJsonAsync(publicSearch);
        Assert.Single(publicSearchJson.RootElement.EnumerateArray());

        var mySearch = await client.GetAsync("/api/notes/mine?search=Consensus");
        mySearch.EnsureSuccessStatusCode();
        var mySearchJson = await ReadJsonAsync(mySearch);
        Assert.Single(mySearchJson.RootElement.EnumerateArray());

        var itemSearch = await client.GetAsync($"/api/notes/{notebookId}/items?search=leader");
        itemSearch.EnsureSuccessStatusCode();
        var itemSearchJson = await ReadJsonAsync(itemSearch);
        Assert.Single(itemSearchJson.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task NotebookEndpoints_SearchIsCaseInsensitive()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"search-case+{Guid.NewGuid():N}@example.com", "203.0.113.100");
        var marker = Guid.NewGuid().ToString("N")[..8];
        var title = $"Distributed Systems {marker}";
        var description = $"Consensus Notes {marker}";
        var content = $"Leader Election {marker}";
        var notebookId = await CreateNotebookAsync(client, title, "public", description);
        await CreatePageAsync(client, notebookId, null, "Raft Intro", 1, content);

        using var anonymous = _factory.CreateClient();
        var publicSearch = await anonymous.GetAsync($"/api/notes/public?search={marker.ToLowerInvariant()}");
        publicSearch.EnsureSuccessStatusCode();
        var publicSearchJson = await ReadJsonAsync(publicSearch);
        Assert.Single(publicSearchJson.RootElement.EnumerateArray());

        var mySearch = await client.GetAsync($"/api/notes/mine?search={marker.ToLowerInvariant()}");
        mySearch.EnsureSuccessStatusCode();
        var mySearchJson = await ReadJsonAsync(mySearch);
        Assert.Single(mySearchJson.RootElement.EnumerateArray());

        var itemSearch = await client.GetAsync($"/api/notes/{notebookId}/items?search={marker.ToLowerInvariant()}");
        itemSearch.EnsureSuccessStatusCode();
        var itemSearchJson = await ReadJsonAsync(itemSearch);
        Assert.Single(itemSearchJson.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task ReorderNotebookItems_RejectsCycles()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"cycle+{Guid.NewGuid():N}@example.com", "203.0.113.88");
        var notebookId = await CreateNotebookAsync(client, "Cycle Notes", "private");
        var parentId = await CreateFolderAsync(client, notebookId, "Parent", 1);
        var childId = await CreateFolderAsync(client, notebookId, "Child", 1, parentId);

        var reorder = await SendWithCsrfAsync(client, HttpMethod.Put, $"/api/notes/{notebookId}/items/reorder", new
        {
            items = new object[]
            {
                new
                {
                    itemId = parentId,
                    parentId = childId,
                    sortOrder = 1
                }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, reorder.StatusCode);
    }

    [Fact]
    public async Task FavoriteEndpoints_UpdateStatusAndNotebookResponses()
    {
        using var owner = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(owner, $"favorite-owner+{Guid.NewGuid():N}@example.com", "203.0.113.89");
        var notebookId = await CreateNotebookAsync(owner, "Favorite Notes", "public");

        using var other = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(other, $"favorite-other+{Guid.NewGuid():N}@example.com", "203.0.113.90");

        var addFavorite = await SendWithCsrfAsync(other, HttpMethod.Post, $"/api/notes/{notebookId}/favorite", new { });
        addFavorite.EnsureSuccessStatusCode();
        var favoriteJson = await ReadJsonAsync(addFavorite);
        Assert.True(favoriteJson.RootElement.GetProperty("isFavorited").GetBoolean());
        Assert.Equal(1, favoriteJson.RootElement.GetProperty("favoriteCount").GetInt32());

        var myNotes = await other.GetAsync("/api/notes/mine?search=Favorite");
        myNotes.EnsureSuccessStatusCode();

        var detail = await owner.GetAsync($"/api/notes/{notebookId}");
        detail.EnsureSuccessStatusCode();
        var detailJson = await ReadJsonAsync(detail);
        Assert.Equal(1, detailJson.RootElement.GetProperty("favoriteCount").GetInt32());
        Assert.False(detailJson.RootElement.GetProperty("isFavoritedByMe").GetBoolean());

        var detailAsFavoritingUser = await other.GetAsync($"/api/notes/{notebookId}");
        detailAsFavoritingUser.EnsureSuccessStatusCode();
        var favoritingUserDetailJson = await ReadJsonAsync(detailAsFavoritingUser);
        Assert.True(favoritingUserDetailJson.RootElement.GetProperty("isFavoritedByMe").GetBoolean());

        using var anonymous = _factory.CreateClient();
        var publicList = await anonymous.GetAsync("/api/notes/public?search=Favorite");
        publicList.EnsureSuccessStatusCode();
        var publicListJson = await ReadJsonAsync(publicList);
        var listedNotebook = publicListJson.RootElement.EnumerateArray().Single();
        Assert.Equal(1, listedNotebook.GetProperty("favoriteCount").GetInt32());
        Assert.False(listedNotebook.GetProperty("isFavoritedByMe").GetBoolean());

        var removeFavorite = await SendWithCsrfAsync(other, HttpMethod.Delete, $"/api/notes/{notebookId}/favorite", new { });
        removeFavorite.EnsureSuccessStatusCode();
        var removedFavoriteJson = await ReadJsonAsync(removeFavorite);
        Assert.False(removedFavoriteJson.RootElement.GetProperty("isFavorited").GetBoolean());
        Assert.Equal(0, removedFavoriteJson.RootElement.GetProperty("favoriteCount").GetInt32());
    }

    [Fact]
    public async Task FavoriteEndpoints_RejectPrivateNotebookForNonOwner()
    {
        using var owner = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(owner, $"favorite-private-owner+{Guid.NewGuid():N}@example.com", "203.0.113.92");
        var notebookId = await CreateNotebookAsync(owner, "Private Favorite Notes", "private");

        using var other = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(other, $"favorite-private-other+{Guid.NewGuid():N}@example.com", "203.0.113.93");

        var getFavorite = await other.GetAsync($"/api/notes/{notebookId}/favorite");
        Assert.Equal(HttpStatusCode.Forbidden, getFavorite.StatusCode);

        var addFavorite = await SendWithCsrfAsync(other, HttpMethod.Post, $"/api/notes/{notebookId}/favorite", new { });
        Assert.Equal(HttpStatusCode.Forbidden, addFavorite.StatusCode);
    }

    [Fact]
    public async Task UpdateNotebook_NormalizesPublicationStateByVisibility()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"publish+{Guid.NewGuid():N}@example.com", "203.0.113.91");
        var notebookId = await CreateNotebookAsync(client, "Publish Notes", "unlisted");

        var publicUpdate = await SendWithCsrfAsync(client, HttpMethod.Put, $"/api/notes/{notebookId}", new
        {
            title = "Publish Notes",
            description = "Now public",
            visibility = "public",
            isPublished = false
        });
        publicUpdate.EnsureSuccessStatusCode();
        var publicJson = await ReadJsonAsync(publicUpdate);
        Assert.True(publicJson.RootElement.GetProperty("isPublished").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, publicJson.RootElement.GetProperty("publishedAtUtc").ValueKind);

        var privateUpdate = await SendWithCsrfAsync(client, HttpMethod.Put, $"/api/notes/{notebookId}", new
        {
            title = "Publish Notes",
            description = "Now private",
            visibility = "private",
            isPublished = true
        });
        privateUpdate.EnsureSuccessStatusCode();
        var privateJson = await ReadJsonAsync(privateUpdate);
        Assert.False(privateJson.RootElement.GetProperty("isPublished").GetBoolean());
        Assert.Equal(JsonValueKind.Null, privateJson.RootElement.GetProperty("publishedAtUtc").ValueKind);
    }

    [Fact]
    public async Task CreateNotebook_NormalizesPublicationStateByVisibility()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"create-publish+{Guid.NewGuid():N}@example.com", "203.0.113.94");

        var privateNotebook = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/notes", new
        {
            title = "Private Draft",
            visibility = "private",
            isPublished = true
        });
        privateNotebook.EnsureSuccessStatusCode();
        var privateJson = await ReadJsonAsync(privateNotebook);
        Assert.False(privateJson.RootElement.GetProperty("isPublished").GetBoolean());
        Assert.Equal(JsonValueKind.Null, privateJson.RootElement.GetProperty("publishedAtUtc").ValueKind);

        var publicNotebook = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/notes", new
        {
            title = "Public Note",
            visibility = "public",
            isPublished = false
        });
        publicNotebook.EnsureSuccessStatusCode();
        var publicJson = await ReadJsonAsync(publicNotebook);
        Assert.True(publicJson.RootElement.GetProperty("isPublished").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, publicJson.RootElement.GetProperty("publishedAtUtc").ValueKind);
    }

    [Fact]
    public async Task UpdateNotebook_RegeneratesGloballyUniqueSlugWhenTitleChanges()
    {
        using var ownerA = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(ownerA, $"slug-owner-a+{Guid.NewGuid():N}@example.com", "203.0.113.98");
        var slugSuffix = Guid.NewGuid().ToString("N")[..8];
        var sharedTitle = $"Distributed Systems {slugSuffix}";

        var firstCreate = await SendWithCsrfAsync(ownerA, HttpMethod.Post, "/api/notes", new
        {
            title = sharedTitle,
            visibility = "public"
        });
        firstCreate.EnsureSuccessStatusCode();
        var firstNotebook = await ReadJsonAsync(firstCreate);
        var firstSlug = firstNotebook.RootElement.GetProperty("slug").GetString();
        Assert.False(string.IsNullOrWhiteSpace(firstSlug));

        using var ownerB = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(ownerB, $"slug-owner-b+{Guid.NewGuid():N}@example.com", "203.0.113.99");

        var secondCreate = await SendWithCsrfAsync(ownerB, HttpMethod.Post, "/api/notes", new
        {
            title = "Backend Notes",
            visibility = "public"
        });
        secondCreate.EnsureSuccessStatusCode();
        var secondNotebook = await ReadJsonAsync(secondCreate);
        var secondNotebookId = secondNotebook.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("backend-notes", secondNotebook.RootElement.GetProperty("slug").GetString());

        var update = await SendWithCsrfAsync(ownerB, HttpMethod.Put, $"/api/notes/{secondNotebookId}", new
        {
            title = sharedTitle,
            description = "Renamed notebook",
            visibility = "public"
        });
        update.EnsureSuccessStatusCode();

        var updatedNotebook = await ReadJsonAsync(update);
        var updatedSlug = updatedNotebook.RootElement.GetProperty("slug").GetString();
        Assert.NotEqual(firstSlug, updatedSlug);
        Assert.StartsWith($"{firstSlug}-", updatedSlug, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NotebookLists_OrderByLastActivityAtUtc()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(client, $"last-activity+{Guid.NewGuid():N}@example.com", "203.0.113.95");

        var firstNotebookId = await CreateNotebookAsync(client, "Older Notebook", "public");
        await Task.Delay(25);
        var secondNotebookId = await CreateNotebookAsync(client, "Newer Notebook", "public");

        await Task.Delay(25);
        await CreatePageAsync(client, firstNotebookId, null, "Fresh Page", 1, "new activity");

        var myNotes = await client.GetAsync("/api/notes/mine");
        myNotes.EnsureSuccessStatusCode();
        var myNotesJson = await ReadJsonAsync(myNotes);
        var myNotesArray = myNotesJson.RootElement.EnumerateArray().ToArray();
        Assert.Equal(firstNotebookId, myNotesArray[0].GetProperty("id").GetGuid());
        Assert.Equal(secondNotebookId, myNotesArray[1].GetProperty("id").GetGuid());

        using var anonymous = _factory.CreateClient();
        var publicNotes = await anonymous.GetAsync("/api/notes/public");
        publicNotes.EnsureSuccessStatusCode();
        var publicNotesJson = await ReadJsonAsync(publicNotes);
        var publicNotesArray = publicNotesJson.RootElement.EnumerateArray().ToArray();
        Assert.Equal(firstNotebookId, publicNotesArray[0].GetProperty("id").GetGuid());
        Assert.Equal(secondNotebookId, publicNotesArray[1].GetProperty("id").GetGuid());
    }

    private static async Task RegisterAsync(HttpClient client, string email, string clientIp)
    {
        using var response = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/auth/register", new
        {
            email,
            password = "Password123!",
            displayName = "Yao"
        }, clientIp);

        response.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> SendWithCsrfAsync(
        HttpClient client,
        HttpMethod method,
        string requestUri,
        object body,
        string? clientIp = null)
    {
        var csrf = await GetCsrfTokenAsync(client);
        var request = new HttpRequestMessage(method, requestUri)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);

        if (clientIp is not null)
        {
            request.Headers.Add("X-Forwarded-For", clientIp);
        }

        return await client.SendAsync(request);
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/auth/csrf");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("token").GetString() ?? throw new InvalidOperationException("Missing CSRF token.");
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static async Task<Guid> CreateNotebookAsync(
        HttpClient client,
        string title,
        string visibility,
        string? description = null)
    {
        var response = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/notes", new
        {
            title,
            description,
            visibility
        });
        response.EnsureSuccessStatusCode();

        var notebook = await ReadJsonAsync(response);
        return notebook.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateFolderAsync(
        HttpClient client,
        Guid notebookId,
        string title,
        int sortOrder,
        Guid? parentId = null)
    {
        var response = await SendWithCsrfAsync(client, HttpMethod.Post, $"/api/notes/{notebookId}/items", new
        {
            parentId,
            type = "folder",
            title,
            sortOrder
        });
        response.EnsureSuccessStatusCode();

        var folder = await ReadJsonAsync(response);
        return folder.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<JsonDocument> CreatePageAsync(
        HttpClient client,
        Guid notebookId,
        Guid? parentId,
        string title,
        int sortOrder,
        string plainTextContent)
    {
        var response = await SendWithCsrfAsync(client, HttpMethod.Post, $"/api/notes/{notebookId}/items", new
        {
            parentId,
            type = "page",
            title,
            sortOrder,
            contentJson = CreateDoc(plainTextContent),
            plainTextContent
        });
        response.EnsureSuccessStatusCode();

        return await ReadJsonAsync(response);
    }

    [Fact]
    public async Task GetNotebookItems_IncludeArchived_ReturnsArchivedItemsOnlyForOwner()
    {
        using var owner = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(owner, $"archive-owner+{Guid.NewGuid():N}@example.com", "203.0.113.200");
        var notebookId = await CreateNotebookAsync(owner, "Archive Notes", "public");

        var page = await CreatePageAsync(owner, notebookId, null, "Active Page", 1, "active");
        var pageId = page.RootElement.GetProperty("id").GetGuid();

        // Archive the page
        var archive = await SendWithCsrfAsync(owner, HttpMethod.Post, $"/api/notes/{notebookId}/items/{pageId}/archive", new { });
        archive.EnsureSuccessStatusCode();
        var archivedPage = await ReadJsonAsync(archive);
        Assert.True(archivedPage.RootElement.GetProperty("isArchived").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, archivedPage.RootElement.GetProperty("archivedAtUtc").ValueKind);

        // Owner can see archived items with includeArchived=true
        var ownerItemsWithArchived = await owner.GetAsync($"/api/notes/{notebookId}/items?includeArchived=true");
        ownerItemsWithArchived.EnsureSuccessStatusCode();
        var ownerItemsJson = await ReadJsonAsync(ownerItemsWithArchived);
        var ownerItems = ownerItemsJson.RootElement.EnumerateArray().ToArray();
        Assert.Single(ownerItems);
        Assert.True(ownerItems[0].GetProperty("isArchived").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, ownerItems[0].GetProperty("archivedAtUtc").ValueKind);

        // Owner does not see archived items without includeArchived
        var ownerItemsWithoutArchived = await owner.GetAsync($"/api/notes/{notebookId}/items");
        ownerItemsWithoutArchived.EnsureSuccessStatusCode();
        var ownerItemsNoArchiveJson = await ReadJsonAsync(ownerItemsWithoutArchived);
        Assert.Empty(ownerItemsNoArchiveJson.RootElement.EnumerateArray());

        // Anonymous user cannot use includeArchived=true
        using var anonymous = _factory.CreateClient();
        var anonymousArchived = await anonymous.GetAsync($"/api/notes/{notebookId}/items?includeArchived=true");
        Assert.Equal(HttpStatusCode.Forbidden, anonymousArchived.StatusCode);

        // Non-owner cannot use includeArchived=true
        using var other = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        await RegisterAsync(other, $"archive-other+{Guid.NewGuid():N}@example.com", "203.0.113.201");
        var otherArchived = await other.GetAsync($"/api/notes/{notebookId}/items?includeArchived=true");
        Assert.Equal(HttpStatusCode.Forbidden, otherArchived.StatusCode);

        // Non-owner sees empty list without includeArchived (archived items hidden)
        var otherItems = await other.GetAsync($"/api/notes/{notebookId}/items");
        otherItems.EnsureSuccessStatusCode();
        var otherItemsJson = await ReadJsonAsync(otherItems);
        Assert.Empty(otherItemsJson.RootElement.EnumerateArray());
    }

    private static object CreateDoc(string text)
    {
        return new
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
        };
    }

    private static object CreateDocWithLeadingHeading(string heading, string body)
    {
        return new
        {
            type = "doc",
            content = new object[]
            {
                new
                {
                    type = "heading",
                    attrs = new { level = 1 },
                    content = new object[]
                    {
                        new { type = "text", text = heading }
                    }
                },
                new
                {
                    type = "paragraph",
                    content = new object[]
                    {
                        new { type = "text", text = body }
                    }
                }
            }
        };
    }
}
