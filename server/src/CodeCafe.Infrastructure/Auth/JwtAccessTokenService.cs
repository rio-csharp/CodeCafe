using System.Security.Claims;
using System.Text;
using CodeCafe.Application.Auth.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CodeCafe.Infrastructure.Auth;

public sealed class JwtAccessTokenService(IOptions<AuthOptions> options) : IAccessTokenService
{
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public AccessToken Issue(Guid userId)
    {
        var authOptions = options.Value;
        var now = DateTimeOffset.UtcNow;
        var expiresAtUtc = now.AddMinutes(authOptions.AccessTokenLifetimeMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
                [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())]
            ),
            Issuer = authOptions.Jwt.Issuer,
            Audience = authOptions.Jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAtUtc.UtcDateTime,
            SigningCredentials = new SigningCredentials(SigningKey(authOptions), SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(_tokenHandler.CreateToken(descriptor), expiresAtUtc);
    }

    public async Task<ValidatedAccessToken?> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        var authOptions = options.Value;
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = authOptions.Jwt.Issuer,
            ValidAudience = authOptions.Jwt.Audience,
            IssuerSigningKey = SigningKey(authOptions),
            // A small skew keeps tokens valid across clock drift between servers.
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        TokenValidationResult result;
        try
        {
            result = await _tokenHandler.ValidateTokenAsync(token, validationParameters);
        }
        catch (ArgumentException)
        {
            return null;
        }

        if (
            !result.IsValid
            || result.ClaimsIdentity?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value is not { } subject
            || !Guid.TryParse(subject, out var userId)
            || result.SecurityToken is not JsonWebToken { IssuedAt: var issuedAt }
        )
        {
            return null;
        }

        return new ValidatedAccessToken(userId, new DateTimeOffset(issuedAt, TimeSpan.Zero));
    }

    private static SymmetricSecurityKey SigningKey(AuthOptions authOptions)
        => new(Encoding.UTF8.GetBytes(authOptions.Jwt.SigningKey));
}
