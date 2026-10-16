using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.PersonalAccessTokens.Shared;
using CodeCafe.Application.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeCafe.Infrastructure.Authentication;

public sealed class CodeCafeAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "CodeCafe";

    private const string BearerPrefix = "Bearer ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ReadBearerToken();
        if (token is null)
            return AuthenticateResult.NoResult();

        // Personal access tokens (MCP clients, scripts, CI) are database-backed instead of
        // JWTs, so they bypass JWT validation — and the PasswordChangedAtUtc check below:
        // revoking them is manual, via the tokens endpoint.
        if (PersonalAccessTokenHash.IsPersonalAccessToken(token))
            return await AuthenticatePersonalAccessTokenAsync(token);

        // Resolved lazily; when no IAccessTokenService is registered, [Authorize] endpoints
        // keep challenging anonymously instead of failing.
        var tokenService = Context.RequestServices.GetService<IAccessTokenService>();
        if (tokenService is null)
            return AuthenticateResult.NoResult();

        var validated = await tokenService.ValidateAsync(token, Context.RequestAborted);
        if (validated is null)
            return AuthenticateResult.NoResult();

        // Password changes must invalidate outstanding access tokens; the token is stateless,
        // so the check compares its iat against the user's PasswordChangedAtUtc. A deleted
        // user fails here too, surfacing as a clean 401 instead of a mid-request 404.
        var users = Context.RequestServices.GetService<IUserRepository>();
        var user = users is null
            ? null
            : await users.FindByIdAsync(validated.UserId, Context.RequestAborted);
        if (user is null || !user.IsAccessTokenCurrent(validated.IssuedAtUtc))
            return AuthenticateResult.NoResult();

        return SuccessTicket(validated.UserId);
    }

    private async Task<AuthenticateResult> AuthenticatePersonalAccessTokenAsync(string token)
    {
        // Resolved lazily for the same reason as IAccessTokenService above.
        var tokens = Context.RequestServices.GetService<IPersonalAccessTokenRepository>();
        if (tokens is null)
            return AuthenticateResult.NoResult();

        var pat = await tokens.FindByHashAsync(
            PersonalAccessTokenHash.Compute(token),
            Context.RequestAborted
        );
        if (pat is null || !pat.IsActive(DateTimeOffset.UtcNow))
            return AuthenticateResult.NoResult();

        // A deleted user must fail here too, exactly like the JWT path.
        var users = Context.RequestServices.GetService<IUserRepository>();
        var user = users is null
            ? null
            : await users.FindByIdAsync(pat.UserId, Context.RequestAborted);
        if (user is null)
            return AuthenticateResult.NoResult();

        return SuccessTicket(pat.UserId);
    }

    private AuthenticateResult SuccessTicket(Guid userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }

    private string? ReadBearerToken()
    {
        var header = Request.Headers.Authorization.ToString();
        return header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? header[BearerPrefix.Length..].Trim()
            : null;
    }

    // Challenges and forbids share the Result error envelope with the rest of the API.
    // The WWW-Authenticate header tells Bearer clients (including MCP clients, per RFC 6750)
    // how to authenticate.
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        => WriteErrorAsync(
            StatusCodes.Status401Unauthorized,
            "unauthorized",
            "Authentication is required. Send a Bearer token in the Authorization header.",
            ErrorKind.Unauthorized,
            challenge: true);

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        => WriteErrorAsync(
            StatusCodes.Status403Forbidden,
            "forbidden",
            "You do not have permission to perform this operation.",
            ErrorKind.Forbidden,
            challenge: false);

    private async Task WriteErrorAsync(int statusCode, string code, string message, ErrorKind kind, bool challenge)
    {
        Response.StatusCode = statusCode;
        if (challenge)
            Response.Headers.WWWAuthenticate = $"{BearerPrefix.Trim()} realm=\"codecafe\"";

        await Response.WriteAsJsonAsync(
            Result.Failure<object>(new Error(code, message, kind)),
            ErrorJsonOptions);
    }

    private static readonly System.Text.Json.JsonSerializerOptions ErrorJsonOptions =
        new(System.Text.Json.JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };
}
