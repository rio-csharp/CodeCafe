using CodeCafe.WebApi.Mcp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CodeCafe.WebApi.Auth;

[ApiController]
public sealed class DynamicClientRegistrationController(
    IOpenIddictApplicationManager applicationManager,
    IOptions<McpOptions> mcpOptionsAccessor,
    IOptions<AuthorizationServerOptions> authorizationServerOptionsAccessor)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("~/connect/register")]
    [Produces("application/json")]
    public async Task<IActionResult> Register(
        [FromBody] DynamicClientRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RedirectUris.Length == 0)
        {
            return BadRequest(CreateRegistrationError("invalid_redirect_uri", "At least one redirect URI is required."));
        }

        if (!string.IsNullOrWhiteSpace(request.TokenEndpointAuthMethod)
            && !string.Equals(request.TokenEndpointAuthMethod, "none", StringComparison.Ordinal))
        {
            return BadRequest(CreateRegistrationError("invalid_client_metadata", "Only public clients using token_endpoint_auth_method 'none' are supported."));
        }

        if (!string.IsNullOrWhiteSpace(request.ApplicationType)
            && !string.Equals(request.ApplicationType, "native", StringComparison.Ordinal))
        {
            return BadRequest(CreateRegistrationError("invalid_client_metadata", "Only native applications are supported."));
        }

        if (request.ResponseTypes.Length > 0
            && request.ResponseTypes.Any(type => !string.Equals(type, "code", StringComparison.Ordinal)))
        {
            return BadRequest(CreateRegistrationError("invalid_client_metadata", "Only the authorization code response type is supported."));
        }

        if (request.GrantTypes.Length > 0
            && request.GrantTypes.Any(type => !string.Equals(type, "authorization_code", StringComparison.Ordinal)
                && !string.Equals(type, "refresh_token", StringComparison.Ordinal)))
        {
            return BadRequest(CreateRegistrationError("invalid_client_metadata", "Only authorization_code and refresh_token grant types are supported."));
        }

        var normalizedRedirectUris = new List<string>();
        foreach (var redirectUri in request.RedirectUris.Distinct(StringComparer.Ordinal))
        {
            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var redirectUriValue)
                || !OpenIddictClientRegistration.IsSupportedLoopbackRedirectUri(redirectUriValue))
            {
                return BadRequest(CreateRegistrationError(
                    "invalid_redirect_uri",
                    $"Redirect URI '{redirectUri}' must be an HTTP loopback URI on localhost, 127.0.0.1, or ::1."));
            }

            normalizedRedirectUris.Add(OpenIddictClientRegistration.NormalizeRedirectUri(redirectUriValue).AbsoluteUri);
        }

        var clientId = $"codecafe-{Guid.NewGuid():N}";
        var descriptor = OpenIddictClientRegistration.CreatePublicNativeDescriptor(
            clientId,
            request.ClientName,
            normalizedRedirectUris,
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

    private static OAuthErrorResponse CreateRegistrationError(string error, string description)
        => new(error, description);
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
