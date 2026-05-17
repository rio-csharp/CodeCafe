using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CodeCafe.Application;
using CodeCafe.Application.Common.Interfaces;
using CodeCafe.Infrastructure.Persistence;
using CodeCafe.WebApi.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeCafe.WebApi.Tests.Auth;

public sealed class AuthApiTests : IClassFixture<AuthApiFixture>
{
    private readonly AuthApiFixture _fixture;

    public AuthApiTests(AuthApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AuthFlow_WithValidCsrf_RegistersReadsCurrentUserLogsOutAndLogsIn()
    {
        using var client = _fixture.CreateBrowserClient();

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
    public async Task Login_WithWrongPassword_ReturnsInvalidCredentials()
    {
        using var client = _fixture.CreateBrowserClient();

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
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal("invalid_credentials", problem?.Code);
    }

    [Fact]
    public async Task Register_WithoutCsrfToken_ReturnsInvalidCsrfToken()
    {
        using var client = _fixture.CreateBrowserClient();

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
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal("invalid_csrf_token", problem?.Code);
    }

    [Fact]
    public async Task Register_RateLimitsByClientIp()
    {
        using var client = _fixture.CreateBrowserClient();
        var clientIp = $"198.51.100.{Random.Shared.Next(1, 200)}";

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var response = await RegisterAsync(client, $"limit-{attempt}+{Guid.NewGuid():N}@example.com", clientIp);
            response.EnsureSuccessStatusCode();
        }

        var limited = await RegisterAsync(client, $"limit-4+{Guid.NewGuid():N}@example.com", clientIp);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        var problem = await limited.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal("rate_limited", problem?.Code);
    }

    private static async Task<HttpResponseMessage> RegisterAsync(BrowserClient client, string email, string clientIp)
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

    private static async Task<string> GetCsrfTokenAsync(BrowserClient client)
    {
        using var response = await client.GetAsync("/api/auth/csrf");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("token").GetString() ?? throw new InvalidOperationException("Missing CSRF token.");
    }

    private sealed record AuthResponse(UserResponse User);

    private sealed record UserResponse(Guid Id, string Email, string DisplayName);

    private sealed record ProblemResponse(string Code);
}

public sealed class AuthApiFixture : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private WebApplication? _app;

    public async Task InitializeAsync()
    {
        _connection.Open();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });

        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=codecafe_tests",
            ["Cors:AllowedOrigins:0"] = "http://localhost"
        });

        builder.AddCodeCafeSerilog();
        builder.Services.AddApplication();
        builder.Services.AddSingleton<IDateTimeProvider, TestDateTimeProvider>();
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite(_connection);
        });
        builder.Services.AddWebApiServices(builder.Configuration, builder.Environment);
        builder.Services.AddCodeCafeForwardedHeaders();

        _app = builder.Build();
        _app.UseCodeCafePipeline();

        using var scope = _app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        await _app.StartAsync();
    }

    public BrowserClient CreateBrowserClient()
    {
        return new BrowserClient(_app?.GetTestClient() ?? throw new InvalidOperationException("Test host is not started."));
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }

        _connection.Dispose();
    }
}

public sealed class TestDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class BrowserClient : IDisposable
{
    private readonly HttpClient _client;
    private readonly Dictionary<string, string> _cookies = [];

    public BrowserClient(HttpClient client)
    {
        _client = client;
    }

    public Task<HttpResponseMessage> GetAsync(string requestUri)
    {
        return SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri));
    }

    public async Task<T?> GetFromJsonAsync<T>(string requestUri)
    {
        using var response = await GetAsync(requestUri);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T value)
    {
        return SendAsync(new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(value)
        });
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        if (_cookies.Count > 0)
        {
            request.Headers.TryAddWithoutValidation(
                "Cookie",
                string.Join("; ", _cookies.Select(cookie => $"{cookie.Key}={cookie.Value}")));
        }

        var response = await _client.SendAsync(request);

        foreach (var header in response.Headers.Where(header => header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var value in header.Value)
            {
                StoreCookie(value);
            }
        }

        return response;
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private void StoreCookie(string setCookieHeader)
    {
        var pair = setCookieHeader.Split(';', 2)[0];
        var separatorIndex = pair.IndexOf('=');
        if (separatorIndex <= 0)
        {
            return;
        }

        var name = pair[..separatorIndex];
        var value = pair[(separatorIndex + 1)..];

        if (string.IsNullOrEmpty(value))
        {
            _cookies.Remove(name);
        }
        else
        {
            _cookies[name] = value;
        }
    }
}
