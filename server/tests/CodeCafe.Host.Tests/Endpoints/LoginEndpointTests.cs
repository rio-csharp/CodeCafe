using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CodeCafe.Host.Tests.Endpoints;

public sealed class LoginEndpointTests(CodeCafeFactory factory)
    : IClassFixture<CodeCafeFactory>
{
    [Theory]
    [InlineData("null")]
    [InlineData("{")]
    public async Task InvalidJsonBody_ReturnsValidationEnvelope(string json)
    {
        using var client = factory.CreateClient();
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/auth/login", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.False(result.GetProperty("isSuccess").GetBoolean());
        Assert.Equal("validation_error", result.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Login_WithEmptyBody_ReturnsValidationError()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsync(
            "/api/auth/login",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken
        );
        var error = document.RootElement.GetProperty("error");
        Assert.Equal("validation_error", error.GetProperty("code").GetString());
        Assert.Equal("Validation", error.GetProperty("kind").GetString());
    }
}
