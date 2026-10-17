using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CodeCafe.Application.Auth.PersonalAccessTokens.Shared;
using CodeCafe.Domain.Identity;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CodeCafe.Host.Tests.Mcp;

// The transport defaults to stateful sessions held in process memory, which only works when every
// request reaches the pod that created the session: behind more than one replica without session
// affinity, the stragglers come back as 404 "Session not found" and clients abort the connection.
// CodeCafe keeps no per-session state — each request carries its own bearer token and every tool is
// a static MediatR call — so the endpoint must not mint sessions at all. These tests pin that down
// over the real endpoint rather than over the transport options, because the observable contract is
// what clients depend on.
public sealed class McpStatelessTransportTests(CodeCafeFactory factory) : IClassFixture<CodeCafeFactory>
{
    private const string Endpoint = "/mcp";
    private const string RawToken = "ccp_0123456789abcdef0123456789abcdef0123456789ab";

    private const string InitializeRequest =
        """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"codecafe-tests","version":"1.0"}}}""";

    [Fact]
    public async Task Initialize_Does_Not_Mint_A_Session()
    {
        using var host = AuthenticatedHost();
        using var client = host.CreateClient();

        using var response = await PostAsync(client, InitializeRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // A session id is what makes the next request depend on the pod that answered this one.
        Assert.False(response.Headers.Contains("Mcp-Session-Id"));
    }

    [Fact]
    public async Task Tools_List_Is_Served_Without_A_Session_Header()
    {
        using var host = AuthenticatedHost();
        using var client = host.CreateClient();

        using var response = await PostAsync(client, """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("codecafe_search", body);
    }

    [Fact]
    public async Task Initialized_Notification_Is_Accepted_Without_A_Session_Header()
    {
        using var host = AuthenticatedHost();
        using var client = host.CreateClient();

        using var response = await PostAsync(client, """{"jsonrpc":"2.0","method":"notifications/initialized"}""");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_Calls_Are_Challenged()
    {
        using var host = AuthenticatedHost();
        using var client = host.CreateClient();

        using var response = await PostAsync(client, InitializeRequest, bearer: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private WebApplicationFactory<Program> AuthenticatedHost()
    {
        var user = User.Create("mcp@example.com", "mcp@example.com", "Mcp", "hash");
        var token = PersonalAccessToken.Create(
            user.Id,
            "mcp",
            PersonalAccessTokenHash.Compute(RawToken),
            DateTimeOffset.UtcNow.AddDays(90));

        return factory.WithPersonalAccessTokenRepositories(token, user);
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string json, string? bearer = RawToken)
    {
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        if (bearer is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
