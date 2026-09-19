using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using CodeCafe.Application.Auth.Abstractions;
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
        // Resolved lazily; when no IAccessTokenService is registered, [Authorize] endpoints
        // keep challenging anonymously instead of failing.
        var tokenService = Context.RequestServices.GetService<IAccessTokenService>();
        var token = ReadBearerToken();
        if (tokenService is null || token is null)
            return AuthenticateResult.NoResult();

        var userId = await tokenService.ValidateAsync(token, Context.RequestAborted);
        if (userId is null)
            return AuthenticateResult.NoResult();

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())],
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
