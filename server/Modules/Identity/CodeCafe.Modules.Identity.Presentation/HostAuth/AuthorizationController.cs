using CodeCafe.Modules.Identity.Infrastructure.Identity;
using CodeCafe.Modules.Identity.Presentation.Configuration;
using CodeCafe.Shared.Application.Configuration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace CodeCafe.Modules.Identity.Presentation.Auth;

[ApiController]
public sealed class AuthorizationController(
    UserManager<ApplicationUser> userManager,
    IOptions<AuthorizationServerOptions> authorizationServerOptionsAccessor,
    IOptions<McpOptions> mcpOptionsAccessor,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager)
    : ControllerBase
{
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be resolved.");

        var authenticationResult = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!authenticationResult.Succeeded)
        {
            if (request.HasPromptValue("none"))
            {
                return Forbid(
                    properties: CreateServerErrorProperties("login_required", "The user is not logged in."),
                    authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
            }

            return Redirect(BuildFrontendLoginUrl());
        }

        var user = await userManager.GetUserAsync(authenticationResult.Principal);
        if (user is null)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return Redirect(BuildFrontendLoginUrl());
        }

        if (request.ClientId is null)
        {
            return BadRequest(new OAuthErrorResponse("invalid_client", "The OAuth client id is required."));
        }

        var application = await applicationManager.FindByClientIdAsync(request.ClientId, cancellationToken);
        if (application is null)
        {
            return BadRequest(new OAuthErrorResponse("invalid_client", "The OAuth client application is not registered."));
        }

        var requestedScopes = request.GetScopes().ToArray();
        var unauthorizedScopes = await GetUnauthorizedScopesAsync(application, requestedScopes, cancellationToken);
        if (unauthorizedScopes.Length > 0)
        {
            return Forbid(
                properties: CreateServerErrorProperties(
                    "invalid_scope",
                    $"The OAuth client is not allowed to request: {string.Join(", ", unauthorizedScopes)}."),
                authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
        }

        var principal = await CreatePrincipalAsync(user, requestedScopes, cancellationToken);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be resolved.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            var authenticationResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var subject = authenticationResult.Principal?.GetClaim(Claims.Subject);
            if (string.IsNullOrWhiteSpace(subject))
            {
                return Forbid(
                    properties: CreateServerErrorProperties("invalid_grant", "The token subject is missing."),
                    authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
            }

            var user = await userManager.FindByIdAsync(subject);
            if (user is null)
            {
                return Forbid(
                    properties: CreateServerErrorProperties("invalid_grant", "The token is bound to an account that no longer exists."),
                    authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
            }

            var principal = await CreatePrincipalAsync(user, authenticationResult.Principal!.GetScopes(), cancellationToken);
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new OAuthErrorResponse("unsupported_grant_type", "The requested OAuth grant type is not supported."));
    }

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(
        ApplicationUser user,
        IEnumerable<string> requestedScopes,
        CancellationToken cancellationToken)
    {
        var requestedScopeSet = requestedScopes.ToHashSet(StringComparer.Ordinal);
        var includeIdentityToken = requestedScopeSet.Contains(Scopes.OpenId);
        var includeProfile = requestedScopeSet.Contains(Scopes.Profile);
        var includeEmail = requestedScopeSet.Contains(Scopes.Email);

        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            Claims.Name,
            Claims.Role);

        identity.SetClaim(Claims.Subject, user.Id.ToString());
        identity.SetClaim(Claims.Name, user.DisplayName);
        identity.SetClaim(Claims.Email, user.Email);

        identity.SetDestinations(claim => claim.Type switch
        {
            Claims.Subject when includeIdentityToken => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Name when includeIdentityToken && includeProfile => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Email when includeIdentityToken && includeEmail => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Name or Claims.Email or Claims.Subject => [Destinations.AccessToken],
            _ => []
        });

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(requestedScopeSet);

        var resources = new List<string>();
        await foreach (var resource in scopeManager.ListResourcesAsync(principal.GetScopes(), cancellationToken))
        {
            resources.Add(resource);
        }

        principal.SetResources(resources.Distinct(StringComparer.Ordinal));
        return principal;
    }

    private async Task<string[]> GetUnauthorizedScopesAsync(
        object application,
        IEnumerable<string> requestedScopes,
        CancellationToken cancellationToken)
    {
        var permissions = await applicationManager.GetPermissionsAsync(application, cancellationToken);
        var grantedPermissions = permissions.ToHashSet(StringComparer.Ordinal);
        var protectedScopes = mcpOptionsAccessor.Value.RequiredReadScopes
            .Concat(mcpOptionsAccessor.Value.RequiredWriteScopes)
            .ToHashSet(StringComparer.Ordinal);

        return requestedScopes
            .Where(protectedScopes.Contains)
            .Where(scope => !grantedPermissions.Contains(Permissions.Prefixes.Scope + scope))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private string BuildFrontendLoginUrl()
    {
        var options = authorizationServerOptionsAccessor.Value;
        var baseUri = new Uri(options.FrontendBaseUrl, UriKind.Absolute);
        var uriBuilder = new UriBuilder(new Uri(baseUri, "/login"));
        var returnUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}{HttpContext.Request.QueryString}";
        uriBuilder.Query = $"returnUrl={Uri.EscapeDataString(returnUrl)}";
        return uriBuilder.Uri.AbsoluteUri;
    }

    private static AuthenticationProperties CreateServerErrorProperties(string error, string description)
    {
        return new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        });
    }
}
