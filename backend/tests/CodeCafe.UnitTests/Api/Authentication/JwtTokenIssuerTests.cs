using CodeCafe.Api.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CodeCafe.UnitTests.Api.Authentication;

public sealed class JwtTokenIssuerTests
{
    [Fact]
    public void IssueToken_embeds_username_and_uses_configured_lifetime()
    {
        var now = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new StubTimeProvider(now);
        var issuer = new JwtTokenIssuer(
            Options.Create(new ConfiguredLoginOptions
            {
                Username = "admin",
                Password = "secret",
                JwtSigningKey = "0123456789abcdef0123456789abcdef",
                JwtTokenLifetimeMinutes = 90,
            }),
            timeProvider);

        var token = issuer.IssueToken("admin");
        var principal = new JwtSecurityTokenHandler().ValidateToken(
            token.Token,
            new TokenValidationParameters
            {
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef")),
                NameClaimType = ClaimTypes.Name,
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
            },
            out _);

        Assert.Equal(now.AddMinutes(90), token.ExpiresAtUtc);
        Assert.Equal("admin", principal.Identity?.Name);
    }

    private sealed class StubTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
