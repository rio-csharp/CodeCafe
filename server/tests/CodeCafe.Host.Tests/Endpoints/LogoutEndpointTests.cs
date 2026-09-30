using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CodeCafe.Host.Tests.Endpoints;

public sealed class LogoutEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Logout_WithEmptyBody_ReturnsValidationError()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsync(
            "/api/auth/logout",
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
