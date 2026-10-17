using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CodeCafe.Application.Auth.PersonalAccessTokens.Shared;
using CodeCafe.Domain.Identity;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CodeCafe.Host.Tests.Endpoints;

// Exercises the real authentication handler's PAT path hermetically: the two repositories it
// resolves are swapped for in-memory stubs, so no PostgreSQL is needed.
public sealed class PersonalAccessTokenAuthTests(CodeCafeFactory factory)
    : IClassFixture<CodeCafeFactory>
{
    private const string RawToken = "ccp_0123456789abcdef0123456789abcdef0123456789ab";

    [Fact]
    public async Task Active_Pat_Authenticates_As_Owner()
    {
        var user = User.Create("pat@example.com", "pat@example.com", "Pat", "hash");
        var pat = PersonalAccessToken.Create(user.Id, "mcp", Hash(RawToken), DateTimeOffset.UtcNow.AddDays(90));
        using var host = WithRepositories(pat, user);
        using var client = host.CreateClient();

        using var response = await SendMeAsync(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(user.Id.ToString(), json.GetProperty("value").GetProperty("id").GetString());
    }

    [Fact]
    public async Task Unknown_Pat_Challenges()
    {
        var user = User.Create("pat@example.com", "pat@example.com", "Pat", "hash");
        var pat = PersonalAccessToken.Create(user.Id, "mcp", Hash("ccp_other"), DateTimeOffset.UtcNow.AddDays(90));
        using var host = WithRepositories(pat, user);
        using var client = host.CreateClient();

        using var response = await SendMeAsync(client);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Revoked_Pat_Challenges()
    {
        var user = User.Create("pat@example.com", "pat@example.com", "Pat", "hash");
        var pat = PersonalAccessToken.Create(user.Id, "mcp", Hash(RawToken), DateTimeOffset.UtcNow.AddDays(90));
        pat.Revoke(DateTimeOffset.UtcNow);
        using var host = WithRepositories(pat, user);
        using var client = host.CreateClient();

        using var response = await SendMeAsync(client);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Expired_Pat_Challenges()
    {
        var user = User.Create("pat@example.com", "pat@example.com", "Pat", "hash");
        var pat = PersonalAccessToken.Create(user.Id, "mcp", Hash(RawToken), DateTimeOffset.UtcNow.AddDays(-1));
        using var host = WithRepositories(pat, user);
        using var client = host.CreateClient();

        using var response = await SendMeAsync(client);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Pat_Of_Deleted_User_Challenges()
    {
        var pat = PersonalAccessToken.Create(Guid.CreateVersion7(), "mcp", Hash(RawToken), DateTimeOffset.UtcNow.AddDays(90));
        using var host = WithRepositories(pat, user: null);
        using var client = host.CreateClient();

        using var response = await SendMeAsync(client);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string Hash(string token) => PersonalAccessTokenHash.Compute(token);

    private static Task<HttpResponseMessage> SendMeAsync(HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", RawToken);
        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private WebApplicationFactory<Program> WithRepositories(PersonalAccessToken? pat, User? user)
        => factory.WithPersonalAccessTokenRepositories(pat, user);
}
