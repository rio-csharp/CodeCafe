using System.Net;
using System.Text;
using System.Text.Json;
using CodeCafe.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeCafe.Host.Tests.Endpoints;

public sealed class RegisterEndpointTests(CodeCafeFactory factory)
    : IClassFixture<CodeCafeFactory>
{
    [Fact]
    public async Task Register_WithEmptyBody_ReturnsValidationError()
    {
        // Validation runs in the MediatR pipeline before any database access, so no
        // connection string is needed for this contract check.
        using var client = factory.CreateClient();
        using var response = await client.PostAsync(
            "/api/auth/register",
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

    [Fact]
    public async Task Register_IsRefusedUnlessTheDeploymentOptsIn()
    {
        // The shipped default is off. Because the flag travels appsettings.json ->
        // AuthOptions -> command, this also proves the wiring end to end, and the handler
        // refuses before it opens the database, so the check stays hermetic.
        using var client = factory.CreateClient();
        using var response = await client.PostAsync(
            "/api/auth/register",
            new StringContent(
                """{"email":"yao@example.com","password":"Password123!","displayName":"Yao"}""",
                Encoding.UTF8,
                "application/json"
            ),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken
        );
        var error = document.RootElement.GetProperty("error");
        Assert.Equal("registration_disabled", error.GetProperty("code").GetString());
        Assert.Equal("Forbidden", error.GetProperty("kind").GetString());
    }

    [Fact]
    public void RegistrationFlagFollowsConfiguration()
    {
        // AuthOptions uses init-only setters. If the binder could not write them, the option
        // would silently stay at its default and an environment that opted in would still
        // refuse sign-ups, so the binding itself is worth an assertion.
        using var enabled = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(
                (_, config) =>
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?> { ["Auth:RegistrationEnabled"] = "true" }
                    )
            )
        );

        Assert.True(enabled.Services.GetRequiredService<IOptions<AuthOptions>>().Value.RegistrationEnabled);
        Assert.False(
            factory.Services.GetRequiredService<IOptions<AuthOptions>>().Value.RegistrationEnabled
        );
    }
}
