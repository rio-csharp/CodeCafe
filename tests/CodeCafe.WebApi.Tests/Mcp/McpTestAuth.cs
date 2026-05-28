using CodeCafe.WebApi.Tests.Auth;
using Microsoft.AspNetCore.WebUtilities;
using ModelContextProtocol.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace CodeCafe.WebApi.Tests.Mcp;

internal static class McpTestAuth
{
    public static async Task<HttpClient> CreateBearerClientAsync(
        AuthApiFactory factory,
        HttpClient authenticatedClient,
        params string[] scopes)
    {
        var accessToken = await CreateAccessTokenAsync(factory, authenticatedClient, scopes);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }

    public static async Task<McpClient> CreateMcpClientAsync(
        AuthApiFactory factory,
        HttpClient authenticatedClient,
        params string[] scopes)
    {
        var client = await CreateBearerClientAsync(factory, authenticatedClient, scopes);
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(client.BaseAddress!, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp
            },
            client);

        return await McpClient.CreateAsync(transport);
    }

    private static async Task<string> CreateAccessTokenAsync(
        AuthApiFactory factory,
        HttpClient authenticatedClient,
        params string[] scopes)
    {
        var codeVerifier = CreateCodeVerifier();
        var codeChallenge = CreateCodeChallenge(codeVerifier);
        var scope = string.Join(' ', scopes);

        var authorizeUri =
            $"/connect/authorize?client_id={Uri.EscapeDataString(factory.McpClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(factory.McpClientRedirectUri)}" +
            "&response_type=code" +
            $"&scope={Uri.EscapeDataString(scope)}" +
            $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
            "&code_challenge_method=S256" +
            "&state=test-state";

        using var authorizeResponse = await authenticatedClient.GetAsync(authorizeUri);
        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);

        var location = authorizeResponse.Headers.Location
            ?? throw new InvalidOperationException("Missing authorization redirect.");
        var query = QueryHelpers.ParseQuery(location.Query);
        var code = query["code"].ToString();
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Missing authorization code.");
        }

        using var tokenClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        using var tokenResponse = await tokenClient.PostAsync(
            "/connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = factory.McpClientId,
                ["code"] = code,
                ["redirect_uri"] = factory.McpClientRedirectUri,
                ["code_verifier"] = codeVerifier
            }));
        tokenResponse.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Missing access token.");
    }

    private static string CreateCodeVerifier()
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }

    private static string CreateCodeChallenge(string codeVerifier)
    {
        return Base64UrlEncode(SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(codeVerifier)));
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
