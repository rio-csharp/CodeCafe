using CodeCafe.Contracts.Auth;
using Microsoft.AspNetCore.Http;

namespace CodeCafe.IntegrationTests;

public sealed class AuthEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Session_is_unauthenticated_before_login()
    {
        var client = factory.CreateClient();

        var session = await client.GetFromJsonAsync<LoginSessionResponse>("/api/auth/session");

        Assert.NotNull(session);
        Assert.False(session.IsAuthenticated);
        Assert.Null(session.Username);
    }

    [Fact]
    public async Task Login_sets_authenticated_session_and_logout_clears_it()
    {
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-user", "test-password"));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loggedInSession = await client.GetFromJsonAsync<LoginSessionResponse>("/api/auth/session");

        Assert.NotNull(loggedInSession);
        Assert.True(loggedInSession.IsAuthenticated);
        Assert.Equal("test-user", loggedInSession.Username);

        var logoutResponse = await client.PostAsync("/api/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var loggedOutSession = await client.GetFromJsonAsync<LoginSessionResponse>("/api/auth/session");

        Assert.NotNull(loggedOutSession);
        Assert.False(loggedOutSession.IsAuthenticated);
        Assert.Null(loggedOutSession.Username);
    }

    [Fact]
    public async Task Login_rejects_invalid_credentials()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("bad-user", "bad-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_is_rate_limited_after_five_attempts_per_hour()
    {
        using var isolatedFactory = factory.WithEnvironment("Testing");
        var client = isolatedFactory.CreateClient();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("bad-user", "bad-password"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var throttledResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("bad-user", "bad-password"));

        Assert.Equal((HttpStatusCode)StatusCodes.Status429TooManyRequests, throttledResponse.StatusCode);
    }

    [Fact]
    public async Task Notes_endpoints_are_public()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/notes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Notes_settings_require_authentication()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/notes/settings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
