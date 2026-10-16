using CodeCafe.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace CodeCafe.Infrastructure.Tests.Auth;

public sealed class JwtAccessTokenServiceTests
{
    private const string SigningKey = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private readonly JwtAccessTokenService _service = CreateService(SigningKey);

    [Fact]
    public async Task Issue_Then_Validate_RoundTripsUserId()
    {
        var userId = Guid.CreateVersion7();

        var token = _service.Issue(userId);

        Assert.NotEmpty(token.Value);
        Assert.True(token.ExpiresAtUtc > DateTimeOffset.UtcNow);
        var validated = await _service.ValidateAsync(token.Value, TestContext.Current.CancellationToken);
        Assert.Equal(userId, validated!.UserId);
        Assert.True(validated.IssuedAtUtc > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task Validate_ReturnsNull_ForTamperedToken()
    {
        var token = _service.Issue(Guid.CreateVersion7());
        var tampered = token.Value[..^2] + "xx";

        Assert.Null(await _service.ValidateAsync(tampered, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Validate_ReturnsNull_ForGarbage()
    {
        Assert.Null(await _service.ValidateAsync("not-a-jwt", TestContext.Current.CancellationToken));
        Assert.Null(await _service.ValidateAsync("", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Validate_ReturnsNull_WhenSignedWithADifferentKey()
    {
        var token = _service.Issue(Guid.CreateVersion7());
        var otherService = CreateService(new string('f', 64));

        Assert.Null(await otherService.ValidateAsync(token.Value, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Validate_ReturnsNull_ForExpiredToken()
    {
        var service = CreateService(SigningKey, accessTokenLifetimeMinutes: -1);
        var token = service.Issue(Guid.CreateVersion7());

        Assert.Null(await service.ValidateAsync(token.Value, TestContext.Current.CancellationToken));
    }

    private static JwtAccessTokenService CreateService(string signingKey, int accessTokenLifetimeMinutes = 15)
        => new(
            Options.Create(
                new AuthOptions
                {
                    AccessTokenLifetimeMinutes = accessTokenLifetimeMinutes,
                    Jwt = new JwtOptions { SigningKey = signingKey },
                }
            )
        );
}
