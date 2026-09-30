using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CodeCafe.Host.Tests.Endpoints;

public sealed class OpenApiContractTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly (string Method, string Path)[] ContractOperations =
    [
        ("post", "/api/auth/register"),
        ("post", "/api/auth/login"),
        ("post", "/api/auth/refresh"),
        ("post", "/api/auth/logout"),
        ("get", "/api/auth/me"),
        ("patch", "/api/auth/me"),
        ("post", "/api/auth/change-password"),

        ("post", "/api/notebooks"),
        ("post", "/api/notebooks/import"),
        ("patch", "/api/notebooks/{idOrSlug}"),
        ("delete", "/api/notebooks/{idOrSlug}"),
        ("post", "/api/notebooks/{idOrSlug}/slug"),
        ("get", "/api/notebooks/slugs/{slug}"),
        ("post", "/api/notebooks/{idOrSlug}/access-code"),
        ("post", "/api/notebooks/{idOrSlug}/favorite"),
        ("post", "/api/notebooks/{idOrSlug}/shares"),
        ("delete", "/api/notebooks/{idOrSlug}/shares/{userId}"),
        ("put", "/api/notebooks/{idOrSlug}/tags"),

        ("get", "/api/notebooks"),
        ("get", "/api/notebooks/{idOrSlug}"),
        ("get", "/api/notebooks/{idOrSlug}/tree"),
        ("get", "/api/notebooks/{idOrSlug}/export"),

        ("post", "/api/notebooks/{idOrSlug}/pages"),
        ("get", "/api/notebooks/{idOrSlug}/pages/by-path"),

        ("get", "/api/pages/{pageId}"),
        ("patch", "/api/pages/{pageId}"),
        ("post", "/api/pages/{pageId}/move"),
        ("delete", "/api/pages/{pageId}"),
        ("post", "/api/pages/{pageId}/favorite"),
        ("get", "/api/pages/favorites"),
        ("post", "/api/pages/{pageId}/shares"),
        ("delete", "/api/pages/{pageId}/shares/{userId}"),

        ("post", "/api/pages/{pageId}/blocks"),
        ("post", "/api/pages/{pageId}/blocks/batch"),
        ("patch", "/api/pages/{pageId}/blocks/{blockId}"),
        ("delete", "/api/pages/{pageId}/blocks/{blockId}"),
        ("post", "/api/pages/{pageId}/blocks/{blockId}/move"),

        ("get", "/api/pages/{pageId}/revisions"),
        ("post", "/api/pages/{pageId}/revisions/restore"),
        ("get", "/api/pages/{pageId}/blocks/{blockId}/revisions"),
        ("post", "/api/pages/{pageId}/blocks/{blockId}/revisions/restore"),

        ("get", "/api/trash"),
        ("post", "/api/trash/{notebookId}/restore"),
        ("delete", "/api/trash/{notebookId}"),
        ("delete", "/api/trash"),
        ("get", "/api/notebooks/{idOrSlug}/trash"),
        ("post", "/api/trash/pages/{pageId}/restore"),
        ("delete", "/api/trash/pages/{pageId}"),

        ("get", "/api/search"),

        ("post", "/api/notebooks/{idOrSlug}/ai/chat"),
    ];

    [Fact]
    public async Task OpenApi_Document_Contains_Every_Contract_Operation()
    {
        using var client = factory.CreateClient();

        var json = await client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var paths = document.RootElement.GetProperty("paths");
        foreach (var (method, path) in ContractOperations)
        {
            Assert.True(
                paths.TryGetProperty(path, out var operations)
                    && operations.EnumerateObject().Any(operation => operation.Name == method),
                $"Missing from OpenAPI document: {method.ToUpperInvariant()} {path}");
        }
    }

    [Fact]
    public async Task Protected_Operation_Documents_Error_Responses()
    {
        using var client = factory.CreateClient();

        var json = await client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var responses = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/pages/{pageId}")
            .GetProperty("patch")
            .GetProperty("responses");

        foreach (var status in new[] { "400", "401", "403", "404", "409", "500" })
            Assert.True(responses.TryGetProperty(status, out _), $"Missing documented response: {status}");
    }

    [Fact]
    public async Task Rate_Limited_Operation_Documents_429()
    {
        using var client = factory.CreateClient();

        var json = await client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var responses = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/auth/login")
            .GetProperty("post")
            .GetProperty("responses");

        Assert.True(responses.TryGetProperty("429", out _), "Rate-limited operations must document 429.");
    }

    [Fact]
    public async Task Ai_Chat_Documents_Server_Sent_Events()
    {
        using var client = factory.CreateClient();

        var json = await client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var content = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/notebooks/{idOrSlug}/ai/chat")
            .GetProperty("post")
            .GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content");

        Assert.True(content.TryGetProperty("text/event-stream", out _), "AI chat must document text/event-stream.");
    }
}
