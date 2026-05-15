using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CodeCafe.Api.Authentication;

public sealed class JwtTokenIssuer(
    IOptions<ConfiguredLoginOptions> configuredLoginOptions,
    TimeProvider timeProvider) : IJwtTokenIssuer
{
    public IssuedAuthToken IssueToken(string username)
    {
        var configuredOptions = configuredLoginOptions.Value;
        var nowUtc = timeProvider.GetUtcNow();
        var expiresAtUtc = nowUtc.AddMinutes(configuredOptions.JwtTokenLifetimeMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuredOptions.JwtSigningKey));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var descriptor = new SecurityTokenDescriptor
        {
            Expires = expiresAtUtc.UtcDateTime,
            IssuedAt = nowUtc.UtcDateTime,
            NotBefore = nowUtc.UtcDateTime,
            SigningCredentials = signingCredentials,
            Subject = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, username)],
                JwtBearerDefaults.AuthenticationScheme),
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(descriptor);

        return new IssuedAuthToken(tokenHandler.WriteToken(token), expiresAtUtc);
    }
}
