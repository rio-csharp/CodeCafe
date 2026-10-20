using System.Net;
using System.Net.Http.Headers;
using CodeCafe.Application.Auth.PersonalAccessTokens.Shared;
using CodeCafe.Application.Common.Security;
using CodeCafe.Domain.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Host.Tests.Endpoints;

public sealed class ImportUploadEndpointTests(CodeCafeFactory factory) : IClassFixture<CodeCafeFactory>
{
    [Fact]
    public async Task Upload_WithoutAToken_IsUnauthorized()
    {
        using var client = factory.CreateClient();
        using var content = Multipart();

        using var response = await client.PostAsync(
            "/api/import-uploads/pages",
            content,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // The endpoint accepts the short-lived capability, never the MCP client's own credential.
    // The PAT is stubbed so the default bearer handler succeeds without a database; the upload
    // endpoint still has to reject it for lacking the upload grant.
    [Fact]
    public async Task Upload_WithAPersonalAccessToken_IsUnauthorized()
    {
        const string rawToken = "ccp_0123456789abcdef0123456789abcdef0123456789ab";
        var user = User.Create("pat@example.com", "pat@example.com", "Pat", "hash");
        var pat = PersonalAccessToken.Create(
            user.Id,
            "mcp",
            PersonalAccessTokenHash.Compute(rawToken),
            DateTimeOffset.UtcNow.AddDays(90)
        );
        using var host = factory.WithPersonalAccessTokenRepositories(pat, user);
        using var client = host.CreateClient();
        using var content = Multipart();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/import-uploads/pages")
        {
            Content = content,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithAMalformedToken_IsUnauthorized()
    {
        using var client = factory.CreateClient();
        using var content = Multipart();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/import-uploads/pages")
        {
            Content = content,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ImportUploadTokens.Prefix + "not-a-jwt"
        );

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithATamperedToken_IsUnauthorized()
    {
        using var client = factory.CreateClient();
        var tokens = factory.Services.GetRequiredService<IImportUploadTokenService>();
        var issued = tokens.Issue(
            new ImportUploadGrant(ImportUploadKind.Page, Guid.CreateVersion7(), Guid.CreateVersion7(), null, "note.md", 1024)
        );
        var tampered = issued.Value[..^1] + (issued.Value[^1] == 'A' ? 'B' : 'A');
        using var content = Multipart();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/import-uploads/pages")
        {
            Content = content,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tampered);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // A notebook token cannot be replayed against the page endpoint; the check is before any
    // database access, so this stays hermetic.
    [Fact]
    public async Task Upload_WithANotebookTokenOnThePageEndpoint_IsRejected()
    {
        using var client = factory.CreateClient();
        var tokens = factory.Services.GetRequiredService<IImportUploadTokenService>();
        var issued = tokens.Issue(
            new ImportUploadGrant(ImportUploadKind.Notebook, Guid.CreateVersion7(), null, null, "note.md", 1024)
        );
        using var content = Multipart();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/import-uploads/pages")
        {
            Content = content,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", issued.Value);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static MultipartFormDataContent Multipart()
    {
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent("# Test\n\nBody"u8.ToArray()), "file", "test.md");
        return content;
    }
}
