using CodeCafe.Modules.Identity.Presentation.Configuration;
using CodeCafe.Application.Common.Configuration;
using CodeCafe.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeCafe.Modules.Identity.Presentation.Auth;

[ApiController]
public sealed class DynamicClientRegistrationController(
    ApplicationDbContext dbContext,
    IOpenIddictApplicationManager applicationManager,
    IOptions<McpOptions> mcpOptionsAccessor,
    IOptions<AuthorizationServerOptions> authorizationServerOptionsAccessor)
    : ControllerBase
{
    [EnableRateLimiting("oauth-registration")]
    [HttpPost("~/connect/register")]
    [Produces("application/json")]
    public async Task<IActionResult> Register(
        [FromBody] DynamicClientRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RedirectUris.Length == 0)
        {
            return BadRequest(new OAuthErrorResponse("invalid_redirect_uri", "At least one redirect URI is required."));
        }

        if (!string.IsNullOrWhiteSpace(request.TokenEndpointAuthMethod)
            && !string.Equals(request.TokenEndpointAuthMethod, "none", StringComparison.Ordinal))
        {
            return BadRequest(new OAuthErrorResponse("invalid_client_metadata", "Only public clients using token_endpoint_auth_method 'none' are supported."));
        }

        if (!string.IsNullOrWhiteSpace(request.ApplicationType)
            && !string.Equals(request.ApplicationType, "native", StringComparison.Ordinal))
        {
            return BadRequest(new OAuthErrorResponse("invalid_client_metadata", "Only native applications are supported."));
        }

        if (request.ResponseTypes.Length > 0
            && request.ResponseTypes.Any(type => !string.Equals(type, "code", StringComparison.Ordinal)))
        {
            return BadRequest(new OAuthErrorResponse("invalid_client_metadata", "Only the authorization code response type is supported."));
        }

        if (request.GrantTypes.Length > 0
            && request.GrantTypes.Any(type => !string.Equals(type, "authorization_code", StringComparison.Ordinal)
                && !string.Equals(type, "refresh_token", StringComparison.Ordinal)))
        {
            return BadRequest(new OAuthErrorResponse("invalid_client_metadata", "Only authorization_code and refresh_token grant types are supported."));
        }

        var normalizedRedirectUris = new HashSet<string>(StringComparer.Ordinal);
        foreach (var redirectUri in request.RedirectUris.Distinct(StringComparer.Ordinal))
        {
            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var redirectUriValue)
                || !OpenIddictClientRegistration.IsSupportedLoopbackRedirectUri(redirectUriValue))
            {
                return BadRequest(new OAuthErrorResponse(
                    "invalid_redirect_uri",
                    $"Redirect URI '{redirectUri}' must be an HTTP loopback URI on localhost, 127.0.0.1, or ::1."));
            }

            normalizedRedirectUris.Add(OpenIddictClientRegistration.NormalizeRedirectUri(redirectUriValue).AbsoluteUri);
        }

        var existingClient = await FindExistingClientAsync(normalizedRedirectUris, request.ClientName, cancellationToken);
        if (existingClient is not null)
        {
            existingClient = await ReconcileExistingClientAsync(
                existingClient,
                normalizedRedirectUris,
                request.ClientName,
                cancellationToken);

            return Ok(new DynamicClientRegistrationResponse
            {
                ApplicationType = "native",
                ClientId = existingClient.ClientId,
                ClientIdIssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ClientName = string.IsNullOrWhiteSpace(existingClient.DisplayName) ? existingClient.ClientId : existingClient.DisplayName,
                GrantTypes = ["authorization_code", "refresh_token"],
                RedirectUris = normalizedRedirectUris.ToArray(),
                ResponseTypes = ["code"],
                TokenEndpointAuthMethod = "none"
            });
        }

        var clientId = OpenIddictClientRegistration.CreateDynamicClientId();
        var descriptor = OpenIddictClientRegistration.CreatePublicNativeDescriptor(
            clientId,
            request.ClientName,
            normalizedRedirectUris,
            OpenIddictClientRegistration.GetDynamicClientAllowedScopes(mcpOptionsAccessor.Value),
            mcpOptionsAccessor.Value,
            authorizationServerOptionsAccessor.Value);

        await applicationManager.CreateAsync(descriptor, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new DynamicClientRegistrationResponse
        {
            ApplicationType = "native",
            ClientId = clientId,
            ClientIdIssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ClientName = descriptor.DisplayName ?? clientId,
            GrantTypes = ["authorization_code", "refresh_token"],
            RedirectUris = descriptor.RedirectUris.Select(uri => uri.AbsoluteUri).ToArray(),
            ResponseTypes = ["code"],
            TokenEndpointAuthMethod = "none"
        });
    }

    private async Task<ExistingDynamicClient?> FindExistingClientAsync(
        IReadOnlyCollection<string> normalizedRedirectUris,
        string? clientName,
        CancellationToken cancellationToken)
    {
        var applications = await dbContext.Set<OpenIddictEntityFrameworkCoreApplication<Guid>>()
            .AsNoTracking()
            .Where(application =>
                application.ClientType == OpenIddictConstants.ClientTypes.Public
                && application.ApplicationType == OpenIddictConstants.ApplicationTypes.Native
                && application.ClientId != null
                && EF.Functions.Like(application.ClientId, "codecafe-%"))
            .ToListAsync(cancellationToken);

        foreach (var application in applications)
        {
            if (!OpenIddictClientRegistration.IsDynamicallyRegisteredClientId(application.ClientId))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(clientName)
                && !string.Equals(application.DisplayName, clientName, StringComparison.Ordinal))
            {
                continue;
            }

            if (!TryParseRedirectUris(application.RedirectUris, out var existingRedirectUris))
            {
                continue;
            }

            if (!existingRedirectUris.SetEquals(normalizedRedirectUris))
            {
                continue;
            }

            return new ExistingDynamicClient(application.ClientId!, application.DisplayName);
        }

        return null;
    }

    private async Task<ExistingDynamicClient> ReconcileExistingClientAsync(
        ExistingDynamicClient existingClient,
        IReadOnlyCollection<string> normalizedRedirectUris,
        string? requestedClientName,
        CancellationToken cancellationToken)
    {
        var application = await applicationManager.FindByClientIdAsync(existingClient.ClientId, cancellationToken)
            ?? throw new InvalidOperationException($"Registered OAuth client '{existingClient.ClientId}' could not be loaded.");

        var existingDescriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(existingDescriptor, application, cancellationToken);

        var desiredDisplayName = string.IsNullOrWhiteSpace(requestedClientName)
            ? existingClient.DisplayName
            : requestedClientName;
        var desiredDescriptor = OpenIddictClientRegistration.CreatePublicNativeDescriptor(
            existingClient.ClientId,
            desiredDisplayName,
            normalizedRedirectUris,
            OpenIddictClientRegistration.GetDynamicClientAllowedScopes(mcpOptionsAccessor.Value),
            mcpOptionsAccessor.Value,
            authorizationServerOptionsAccessor.Value);

        if (OpenIddictClientRegistration.ReconcileDescriptor(existingDescriptor, desiredDescriptor))
        {
            await applicationManager.UpdateAsync(application, existingDescriptor, cancellationToken);
        }

        return new ExistingDynamicClient(existingClient.ClientId, existingDescriptor.DisplayName);
    }
    private static bool TryParseRedirectUris(string? redirectUrisJson, out HashSet<string> redirectUris)
    {
        redirectUris = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(redirectUrisJson))
        {
            return false;
        }

        try
        {
            var values = JsonSerializer.Deserialize<string[]>(redirectUrisJson);
            if (values is null)
            {
                return false;
            }

            redirectUris.UnionWith(values.Where(value => !string.IsNullOrWhiteSpace(value)));
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record ExistingDynamicClient(string ClientId, string? DisplayName);
}

public sealed class DynamicClientRegistrationRequest
{
    [JsonPropertyName("application_type")]
    public string? ApplicationType { get; init; }

    [JsonPropertyName("client_name")]
    [StringLength(200)]
    public string? ClientName { get; init; }

    [JsonPropertyName("grant_types")]
    public string[] GrantTypes { get; init; } = [];

    [JsonPropertyName("redirect_uris")]
    public string[] RedirectUris { get; init; } = [];

    [JsonPropertyName("response_types")]
    public string[] ResponseTypes { get; init; } = [];

    [JsonPropertyName("token_endpoint_auth_method")]
    public string? TokenEndpointAuthMethod { get; init; }
}

public sealed class DynamicClientRegistrationResponse
{
    [JsonPropertyName("application_type")]
    public string ApplicationType { get; init; } = "native";

    [JsonPropertyName("client_id")]
    public string ClientId { get; init; } = string.Empty;

    [JsonPropertyName("client_id_issued_at")]
    public long ClientIdIssuedAt { get; init; }

    [JsonPropertyName("client_name")]
    public string ClientName { get; init; } = string.Empty;

    [JsonPropertyName("grant_types")]
    public string[] GrantTypes { get; init; } = [];

    [JsonPropertyName("redirect_uris")]
    public string[] RedirectUris { get; init; } = [];

    [JsonPropertyName("response_types")]
    public string[] ResponseTypes { get; init; } = [];

    [JsonPropertyName("token_endpoint_auth_method")]
    public string TokenEndpointAuthMethod { get; init; } = "none";
}

public sealed record OAuthErrorResponse(
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("error_description")] string ErrorDescription);
