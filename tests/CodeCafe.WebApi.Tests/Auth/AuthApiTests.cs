using CodeCafe.Infrastructure.Persistence;
using CodeCafe.WebApi.Mcp;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CodeCafe.WebApi.Auth;

namespace CodeCafe.WebApi.Tests.Auth;

public sealed class AuthApiTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AuthApiTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthFlow_WithValidCsrf_RegistersReadsCurrentUserLogsOutAndLogsIn()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var email = $"smoke+{Guid.NewGuid():N}@example.com";
        var registerCsrf = await GetCsrfTokenAsync(client);

        using var register = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(new
            {
                email,
                password = "Password123!",
                displayName = "Yao"
            })
        };
        register.Headers.Add("X-CSRF-TOKEN", registerCsrf);
        register.Headers.Add("X-Forwarded-For", "203.0.113.10");

        var registerResponse = await client.SendAsync(register);
        registerResponse.EnsureSuccessStatusCode();
        AssertAuthCookieSet(registerResponse);

        var currentUser = await client.GetFromJsonAsync<AuthResponse>("/api/auth/me");
        Assert.Equal(email, currentUser?.User.Email);
        Assert.Equal("Yao", currentUser?.User.DisplayName);

        var logoutCsrf = await GetCsrfTokenAsync(client);
        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
        {
            Content = JsonContent.Create(new { })
        };
        logout.Headers.Add("X-CSRF-TOKEN", logoutCsrf);

        var logoutResponse = await client.SendAsync(logout);
        logoutResponse.EnsureSuccessStatusCode();
        AssertAuthCookieExpired(logoutResponse);

        var unauthorizedMe = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedMe.StatusCode);

        var loginCsrf = await GetCsrfTokenAsync(client);
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new
            {
                email,
                password = "Password123!"
            })
        };
        login.Headers.Add("X-CSRF-TOKEN", loginCsrf);

        var loginResponse = await client.SendAsync(login);
        loginResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsEmailAlreadyRegistered()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var email = $"duplicate+{Guid.NewGuid():N}@example.com";
        await RegisterAsync(client, email, "203.0.113.30");

        var response = await RegisterAsync(client, email, "203.0.113.31");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("email_already_registered", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Logout_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var csrf = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
        {
            Content = JsonContent.Create(new { })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenAlreadyAuthenticated_AllowsRepeatedLogin()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var email = $"repeat-login+{Guid.NewGuid():N}@example.com";
        await RegisterAsync(client, email, "203.0.113.32");

        var response = await LoginAsync(client, email, "Password123!", "203.0.113.32");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsInvalidCredentials()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var email = $"wrong-password+{Guid.NewGuid():N}@example.com";
        await RegisterAsync(client, email, "203.0.113.20");

        var csrf = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new
            {
                email,
                password = "WrongPassword123!"
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("invalid_credentials", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Register_WithInvalidBody_ReturnsInvalidRequest()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var csrf = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(new
            {
                email = "bad",
                password = "123",
                displayName = ""
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_request", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Register_WithWhitespaceDisplayName_ReturnsInvalidDisplayName()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var csrf = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(new
            {
                email = $"blank-name+{Guid.NewGuid():N}@example.com",
                password = "Password123!",
                displayName = "   "
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_request", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Register_WithoutCsrfToken_ReturnsInvalidCsrfToken()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(new
            {
                email = $"csrf+{Guid.NewGuid():N}@example.com",
                password = "Password123!",
                displayName = "Yao"
            })
        };
        request.Headers.Add("X-Forwarded-For", $"203.0.113.{Random.Shared.Next(50, 199)}");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_csrf_token", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Register_WithInvalidCsrfToken_ReturnsInvalidCsrfToken()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(new
            {
                email = $"csrf-invalid+{Guid.NewGuid():N}@example.com",
                password = "Password123!",
                displayName = "Yao"
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", "not-a-valid-token");
        request.Headers.Add("X-Forwarded-For", $"203.0.113.{Random.Shared.Next(50, 199)}");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_csrf_token", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task Register_RateLimitsByClientIp()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var clientIp = $"198.51.100.{Random.Shared.Next(1, 200)}";

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var response = await RegisterAsync(client, $"limit-{attempt}+{Guid.NewGuid():N}@example.com", clientIp);
            response.EnsureSuccessStatusCode();
        }

        var limited = await RegisterAsync(client, $"limit-4+{Guid.NewGuid():N}@example.com", clientIp);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("rate_limited", await ReadErrorCodeAsync(limited));
    }

    [Fact]
    public async Task Login_RateLimitsByClientIp()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var clientIp = $"198.51.100.{Random.Shared.Next(1, 200)}";

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            var response = await LoginAsync(
                client,
                $"unknown-{attempt}+{Guid.NewGuid():N}@example.com",
                "WrongPassword123!",
                clientIp);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var limited = await LoginAsync(
            client,
            $"unknown-11+{Guid.NewGuid():N}@example.com",
            "WrongPassword123!",
            clientIp);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("rate_limited", await ReadErrorCodeAsync(limited));
    }

    [Fact]
    public async Task Register_WhenRegistrationDisabled_ReturnsForbidden()
    {
        using var factory = new AuthApiFactory
        {
            RegistrationEnabled = false
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var response = await RegisterAsync(
            client,
            $"disabled+{Guid.NewGuid():N}@example.com",
            "203.0.113.40");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("registration_disabled", await ReadErrorCodeAsync(response));
    }

    [Fact]
    public async Task McpEndpoint_WhenDisabled_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/mcp", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task McpEndpoint_WhenEnabled_AnonymousRequestIsUnauthorized()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true
        };
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/mcp", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task McpGetNotebookTool_ReturnsStructuredNotebookMetadata()
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

        var email = $"mcp+{Guid.NewGuid():N}@example.com";
        await RegisterAsync(client, email, "203.0.113.60");

        var createNotebookResponse = await SendWithCsrfAsync(client, HttpMethod.Post, "/api/notes", new
        {
            title = "MCP Notebook",
            description = "Tool test",
            visibility = "private"
        });
        createNotebookResponse.EnsureSuccessStatusCode();

        using var notebookJson = JsonDocument.Parse(await createNotebookResponse.Content.ReadAsStringAsync());
        var slug = notebookJson.RootElement.GetProperty("slug").GetString() ?? throw new InvalidOperationException("Missing notebook slug.");
        await using var mcpClient = await Mcp.McpTestAuth.CreateMcpClientAsync(factory, client, "notes.read");
        var tools = await mcpClient.ListToolsAsync();
        Assert.Contains(tools, tool => tool.Name == NotesMcpToolNames.GetNotebook);

        var result = await mcpClient.CallToolAsync(
            NotesMcpToolNames.GetNotebook,
            new Dictionary<string, object?> { ["slug"] = slug },
            cancellationToken: CancellationToken.None);

        Assert.NotEqual(true, result.IsError);
        Assert.True(result.StructuredContent.HasValue);
        Assert.Equal("MCP Notebook", result.StructuredContent.Value.GetProperty("title").GetString());
        Assert.Equal(slug, result.StructuredContent.Value.GetProperty("slug").GetString());
        Assert.Equal("private", result.StructuredContent.Value.GetProperty("visibility").GetString());
    }

    [Fact]
    public async Task OpenIddictSeedHostedService_RemovesStaleRedirectUrisAndPermissions()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true
        };
        using var client = factory.CreateClient();

        await client.GetAsync("/health/live");

        using var scope = factory.Services.CreateScope();
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var seedService = scope.ServiceProvider
            .GetServices<IHostedService>()
            .Single(service => service is OpenIddictSeedHostedService);

        var application = await applicationManager.FindByClientIdAsync(factory.McpClientId, CancellationToken.None);
        Assert.NotNull(application);

        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, application!, CancellationToken.None);
        descriptor.RedirectUris.Add(new Uri("http://localhost:9999/callback"));
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "notes.admin");
        await applicationManager.UpdateAsync(application!, descriptor, CancellationToken.None);

        await seedService.StartAsync(CancellationToken.None);

        using var verificationScope = factory.Services.CreateScope();
        var verificationApplicationManager = verificationScope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var refreshedApplication = await verificationApplicationManager.FindByClientIdAsync(factory.McpClientId, CancellationToken.None);
        Assert.NotNull(refreshedApplication);

        var refreshedDescriptor = new OpenIddictApplicationDescriptor();
        await verificationApplicationManager.PopulateAsync(refreshedDescriptor, refreshedApplication!, CancellationToken.None);

        Assert.DoesNotContain(
            refreshedDescriptor.RedirectUris.Select(uri => uri.AbsoluteUri),
            uri => uri == "http://localhost:9999/callback");
        Assert.DoesNotContain(
            refreshedDescriptor.Permissions,
            permission => permission == OpenIddictConstants.Permissions.Prefixes.Scope + "notes.admin");
        Assert.Contains(
            refreshedDescriptor.Permissions,
            permission => permission == OpenIddictConstants.Permissions.Prefixes.Audience + factory.McpAudience);
        Assert.Contains(
            refreshedDescriptor.Permissions,
            permission => permission == OpenIddictConstants.Permissions.Prefixes.Resource + factory.CanonicalMcpResource);
        Assert.Contains(
            refreshedDescriptor.RedirectUris.Select(uri => uri.AbsoluteUri),
            uri => uri == factory.McpClientRedirectUri);
    }

    [Fact]
    public async Task OpenIddictSeedHostedService_ReconcilesScopeResources()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true
        };
        using var client = factory.CreateClient();

        await client.GetAsync("/health/live");

        using var scope = factory.Services.CreateScope();
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        var seedService = scope.ServiceProvider
            .GetServices<IHostedService>()
            .Single(service => service is OpenIddictSeedHostedService);

        var scopeEntry = await scopeManager.FindByNameAsync("notes.read", CancellationToken.None);
        Assert.NotNull(scopeEntry);

        var descriptor = new OpenIddictScopeDescriptor();
        await scopeManager.PopulateAsync(descriptor, scopeEntry!, CancellationToken.None);
        descriptor.Resources.Clear();
        descriptor.Resources.Add("stale-resource");
        await scopeManager.UpdateAsync(scopeEntry!, descriptor, CancellationToken.None);

        await seedService.StartAsync(CancellationToken.None);

        using var verificationScope = factory.Services.CreateScope();
        var verificationScopeManager = verificationScope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        var refreshedScope = await verificationScopeManager.FindByNameAsync("notes.read", CancellationToken.None);
        Assert.NotNull(refreshedScope);

        var refreshedDescriptor = new OpenIddictScopeDescriptor();
        await verificationScopeManager.PopulateAsync(refreshedDescriptor, refreshedScope!, CancellationToken.None);

        Assert.DoesNotContain(refreshedDescriptor.Resources, resource => resource == "stale-resource");
        Assert.Contains(refreshedDescriptor.Resources, resource => resource == factory.McpAudience);
        Assert.Contains(refreshedDescriptor.Resources, resource => resource == factory.CanonicalMcpResource);
    }

    [Fact]
    public async Task Authorize_WithCanonicalMcpResource_RedirectsToFrontendLogin()
    {
        using var factory = new AuthApiFactory
        {
            McpEnabled = true,
            McpClientRedirectUri = "http://localhost:3334/callback"
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var authorizeUrl =
            $"/connect/authorize?response_type=code" +
            $"&client_id={Uri.EscapeDataString(factory.McpClientId)}" +
            $"&code_challenge=test-challenge" +
            $"&code_challenge_method=S256" +
            $"&redirect_uri={Uri.EscapeDataString(factory.McpClientRedirectUri)}" +
            $"&state=test-state" +
            $"&scope={Uri.EscapeDataString("notes.read notes.write offline_access")}" +
            $"&resource={Uri.EscapeDataString(factory.CanonicalMcpResource)}";

        using var response = await client.GetAsync(authorizeUrl);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith($"{factory.FrontendBaseUrl}/login", response.Headers.Location!.AbsoluteUri, StringComparison.Ordinal);
    }

    private static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string clientIp)
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

        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string email,
        string password,
        string clientIp)
    {
        var csrf = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new
            {
                email,
                password
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        request.Headers.Add("X-Forwarded-For", clientIp);

        return await client.SendAsync(request);
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/auth/csrf");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("token").GetString() ?? throw new InvalidOperationException("Missing CSRF token.");
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("code", out var camelCaseCode)
            ? camelCaseCode.GetString()
            : document.RootElement.GetProperty("Code").GetString();
    }

    private static async Task<HttpResponseMessage> SendWithCsrfAsync(
        HttpClient client,
        HttpMethod method,
        string requestUri,
        object body)
    {
        var csrf = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(method, requestUri)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);

        return await client.SendAsync(request);
    }

    private static void AssertAuthCookieSet(HttpResponseMessage response)
    {
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), value =>
            value.StartsWith("CodeCafe.Auth=", StringComparison.Ordinal)
            && !value.StartsWith("CodeCafe.Auth=;", StringComparison.Ordinal));
    }

    private static void AssertAuthCookieExpired(HttpResponseMessage response)
    {
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), value =>
            value.StartsWith("CodeCafe.Auth=", StringComparison.Ordinal)
            && value.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record AuthResponse(UserResponse User);

    private sealed record UserResponse(Guid Id, string Email, string DisplayName);
}

public sealed class AuthApiFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public bool RegistrationEnabled { get; set; } = true;

    public bool McpEnabled { get; set; }

    public string McpAudience { get; set; } = "codecafe-mcp";

    public string AuthorizationServerIssuer { get; set; } = "https://codecafe.test/";

    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    public string McpClientId { get; set; } = "codecafe-claude";

    public string McpClientRedirectUri { get; set; } = "http://localhost/";

    public string CanonicalMcpResource => new Uri(new Uri(AuthorizationServerIssuer, UriKind.Absolute), "/mcp").AbsoluteUri;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:RegistrationEnabled"] = RegistrationEnabled.ToString(),
                ["Mcp:Enabled"] = McpEnabled.ToString(),
                ["Mcp:EndpointPath"] = "/mcp",
                ["Mcp:ProtectedResourceMetadataPath"] = "/.well-known/oauth-protected-resource/mcp",
                ["Mcp:RequireAuthorization"] = "true",
                ["Mcp:RequiredAudience"] = McpAudience,
                ["Mcp:RequiredReadScopes:0"] = "notes.read",
                ["Mcp:RequiredWriteScopes:0"] = "notes.write",
                ["AuthorizationServer:Issuer"] = AuthorizationServerIssuer,
                ["AuthorizationServer:FrontendBaseUrl"] = FrontendBaseUrl,
                ["AuthorizationServer:PublicClients:0:ClientId"] = McpClientId,
                ["AuthorizationServer:PublicClients:0:DisplayName"] = "Claude Code Tests",
                ["AuthorizationServer:PublicClients:0:RedirectUris:0"] = McpClientRedirectUri,
                ["Cors:AllowedOrigins:0"] = "http://localhost"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.UseOpenIddict<Guid>();
            });

            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open();
            }

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }

    public new void Dispose()
    {
        _connection.Dispose();
        base.Dispose();
    }
}
