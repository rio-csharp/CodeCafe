using System.Security.Claims;
using System.Text.Encodings.Web;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CodeCafe.Host.Hosting;

// Authenticates the short-lived capability an MCP client receives for one upload hop. It is a
// separate scheme, so the normal bearer handler never has to consider upload tokens and no
// other endpoint accepts them.
public sealed class ImportUploadAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IImportUploadTokenService tokens) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "CodeCafeImportUpload";

    // The validated grant rides along for the controller; re-parsing the token there would be
    // a second validation with its own chance to drift.
    public const string GrantItemKey = "CodeCafe.ImportUploadGrant";

    private const string BearerPrefix = "Bearer ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = header[BearerPrefix.Length..].Trim();
        if (!token.StartsWith(ImportUploadTokens.Prefix, StringComparison.Ordinal))
        {
            return AuthenticateResult.NoResult();
        }

        var grant = await tokens.ValidateAsync(token, Context.RequestAborted);
        if (grant is null)
        {
            return AuthenticateResult.NoResult();
        }

        Context.Items[GrantItemKey] = grant;
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, grant.UserId.ToString())],
            Scheme.Name
        );
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Bearer realm=\"codecafe-import\"";
        return ErrorResponses.WriteAsync(
            Context,
            new Error(
                "unauthorized",
                "A valid temporary upload token is required.",
                ErrorKind.Unauthorized
            )
        );
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        => ErrorResponses.WriteAsync(
            Context,
            new Error(
                "forbidden",
                "You do not have permission to perform this operation.",
                ErrorKind.Forbidden
            )
        );
}
