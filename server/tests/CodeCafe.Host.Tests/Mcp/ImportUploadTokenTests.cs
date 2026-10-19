using System.Text.Json;
using CodeCafe.Application.Common.Security;
using CodeCafe.Host.Mcp;
using CodeCafe.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeCafe.Host.Tests.Mcp;

public sealed class ImportUploadTokenTests
{
    private const string SigningKey = "codecafe-host-tests-only-signing-key";

    [Fact]
    public async Task Issue_And_Validate_RoundTripsAPageGrant()
    {
        var service = CreateService();
        var grant = new ImportUploadGrant(
            ImportUploadKind.Page,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "/stage-1",
            "001-async-await.md",
            4096
        );

        var token = service.Issue(grant);

        Assert.StartsWith(ImportUploadTokens.Prefix, token.Value);
        Assert.Equal(grant, await service.ValidateAsync(token.Value, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Issue_And_Validate_RoundTripsANotebookGrant()
    {
        var service = CreateService();
        var grant = new ImportUploadGrant(
            ImportUploadKind.Notebook,
            Guid.CreateVersion7(),
            null,
            null,
            "interview-prep.md",
            8192
        );

        var token = service.Issue(grant);

        Assert.Equal(grant, await service.ValidateAsync(token.Value, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Validate_RejectsAnAccessToken()
    {
        var service = CreateService();
        var accessToken = new JwtAccessTokenService(CreateOptions()).Issue(Guid.CreateVersion7());

        Assert.Null(await service.ValidateAsync(accessToken.Value, TestContext.Current.CancellationToken));
    }

    // Prefix alone is not the boundary; the audience is.
    [Fact]
    public async Task Validate_RejectsAnAccessTokenEvenWithTheUploadPrefix()
    {
        var service = CreateService();
        var accessToken = new JwtAccessTokenService(CreateOptions()).Issue(Guid.CreateVersion7());

        Assert.Null(await service.ValidateAsync(
            ImportUploadTokens.Prefix + accessToken.Value,
            TestContext.Current.CancellationToken
        ));
    }

    [Fact]
    public async Task AccessTokenValidator_RejectsAnUploadToken()
    {
        var service = CreateService();
        var uploadToken = service.Issue(NewPageGrant());
        var accessTokens = new JwtAccessTokenService(CreateOptions());

        Assert.Null(await accessTokens.ValidateAsync(uploadToken.Value, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Validate_RejectsATamperedToken()
    {
        var service = CreateService();
        var token = service.Issue(NewPageGrant()).Value;
        var tampered = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');

        Assert.Null(await service.ValidateAsync(tampered, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Validate_RejectsAnExpiredToken()
    {
        var service = CreateService(lifetimeMinutes: -2);
        var token = service.Issue(NewPageGrant());

        Assert.Null(await service.ValidateAsync(token.Value, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Validate_RejectsATokenWithoutTheUploadPrefix()
    {
        var service = CreateService();

        Assert.Null(await service.ValidateAsync("not-a-token", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Validate_RejectsAMalformedUploadToken()
    {
        var service = CreateService();

        Assert.Null(await service.ValidateAsync(
            ImportUploadTokens.Prefix + "not-a-jwt",
            TestContext.Current.CancellationToken
        ));
    }

    [Fact]
    public void Build_ReturnsAnAbsolutePageUploadUrlAndACommand()
    {
        var service = CreateService();
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("api.codes.cafe");
        var grant = NewPageGrant();

        var json = JsonSerializer.SerializeToElement(
            ImportUploadResponse.Build(grant, service, context, "pages")
        );

        Assert.Equal(
            "https://api.codes.cafe/api/import-uploads/pages",
            json.GetProperty("uploadUrl").GetString()
        );
        Assert.StartsWith(ImportUploadTokens.Prefix, json.GetProperty("uploadToken").GetString()!);
        Assert.Equal(grant.MaxBytes, json.GetProperty("maxUploadBytes").GetInt32());
        Assert.Contains(
            "Authorization: Bearer",
            json.GetProperty("example").GetString()!,
            StringComparison.Ordinal
        );
    }

    private static ImportUploadGrant NewPageGrant() => new(
        ImportUploadKind.Page,
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        null,
        "page.md",
        1024
    );

    private static JwtImportUploadTokenService CreateService(int lifetimeMinutes = 10)
        => new(CreateOptions(lifetimeMinutes));

    private static IOptions<AuthOptions> CreateOptions(int lifetimeMinutes = 10) => Options.Create(new AuthOptions
    {
        ImportUploadLifetimeMinutes = lifetimeMinutes,
        Jwt = new JwtOptions { SigningKey = SigningKey },
    });
}
