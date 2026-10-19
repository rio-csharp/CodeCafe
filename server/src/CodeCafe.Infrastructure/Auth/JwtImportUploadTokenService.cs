using System.Globalization;
using System.Security.Claims;
using System.Text;
using CodeCafe.Application.Common.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CodeCafe.Infrastructure.Auth;

// A short-lived JWT for the HTTP upload hop. It reuses the access token signing key — shared
// by every replica — but not its audience, so an upload token cannot be used on normal
// endpoints and an access token cannot be used on the upload endpoint.
public sealed class JwtImportUploadTokenService(IOptions<AuthOptions> options) : IImportUploadTokenService
{
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public ImportUploadToken Issue(ImportUploadGrant grant)
    {
        var authOptions = options.Value;
        var now = DateTimeOffset.UtcNow;
        var expiresAtUtc = now.AddMinutes(authOptions.ImportUploadLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, grant.UserId.ToString()),
            new("kind", grant.Kind == ImportUploadKind.Page ? "page" : "notebook"),
            new("file", grant.FileName),
            new("max", grant.MaxBytes.ToString(CultureInfo.InvariantCulture)),
        };
        if (grant.NotebookId is { } notebookId)
        {
            claims.Add(new Claim("nb", notebookId.ToString()));
        }
        if (grant.ParentPath is { } parentPath)
        {
            claims.Add(new Claim("pp", parentPath));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = authOptions.Jwt.Issuer,
            Audience = ImportUploadTokens.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAtUtc.UtcDateTime,
            SigningCredentials = new SigningCredentials(SigningKey(authOptions), SecurityAlgorithms.HmacSha256),
        };

        return new ImportUploadToken(ImportUploadTokens.Prefix + _tokenHandler.CreateToken(descriptor), expiresAtUtc);
    }

    public async Task<ImportUploadGrant?> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!token.StartsWith(ImportUploadTokens.Prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var authOptions = options.Value;
        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = authOptions.Jwt.Issuer,
            ValidAudience = ImportUploadTokens.Audience,
            IssuerSigningKey = SigningKey(authOptions),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromMinutes(1),
        };

        TokenValidationResult result;
        try
        {
            result = await _tokenHandler.ValidateTokenAsync(token[ImportUploadTokens.Prefix.Length..], validationParameters);
        }
        catch (ArgumentException)
        {
            return null;
        }

        if (!result.IsValid || result.ClaimsIdentity is not { } identity)
        {
            return null;
        }

        if (identity.FindFirst(JwtRegisteredClaimNames.Sub)?.Value is not { } subject
            || !Guid.TryParse(subject, out var userId)
            || identity.FindFirst("kind")?.Value is not { } kind
            || identity.FindFirst("file")?.Value is not { } fileName
            || string.IsNullOrWhiteSpace(fileName)
            || identity.FindFirst("max")?.Value is not { } maxText
            || !int.TryParse(maxText, NumberStyles.None, CultureInfo.InvariantCulture, out var maxBytes)
            || maxBytes <= 0)
        {
            return null;
        }

        Guid? notebookId = null;
        if (identity.FindFirst("nb")?.Value is { } notebookText)
        {
            if (!Guid.TryParse(notebookText, out var parsed))
            {
                return null;
            }
            notebookId = parsed;
        }

        var parentPath = identity.FindFirst("pp")?.Value;

        return kind switch
        {
            "page" when notebookId is not null
                => new ImportUploadGrant(ImportUploadKind.Page, userId, notebookId, parentPath, fileName, maxBytes),
            "notebook" when notebookId is null
                => new ImportUploadGrant(ImportUploadKind.Notebook, userId, null, null, fileName, maxBytes),
            _ => null,
        };
    }

    private static SymmetricSecurityKey SigningKey(AuthOptions authOptions)
        => new(Encoding.UTF8.GetBytes(authOptions.Jwt.SigningKey));
}
