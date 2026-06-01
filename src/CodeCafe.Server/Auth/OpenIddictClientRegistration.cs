using CodeCafe.Server.Configuration;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace CodeCafe.Server.Auth;

internal static class OpenIddictClientRegistration
{
    public static OpenIddictApplicationDescriptor CreatePublicNativeDescriptor(
        string clientId,
        string? displayName,
        IEnumerable<string> redirectUris,
        McpServerOptions mcpOptions,
        AuthorizationServerOptions authorizationServerOptions)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ApplicationType = ApplicationTypes.Native,
            ClientType = ClientTypes.Public,
            ClientId = clientId,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? clientId : displayName
        };

        foreach (var redirectUri in redirectUris)
        {
            if (TryNormalizeRedirectUri(redirectUri, out var normalizedUri))
            {
                descriptor.RedirectUris.Add(normalizedUri);
            }
        }

        descriptor.Permissions.UnionWith(
        [
            Permissions.Endpoints.Authorization,
            Permissions.Endpoints.Token,
            Permissions.GrantTypes.AuthorizationCode,
            Permissions.GrantTypes.RefreshToken,
            Permissions.ResponseTypes.Code
        ]);

        foreach (var scopeName in mcpOptions.RequiredReadScopes.Concat(mcpOptions.RequiredWriteScopes).Distinct(StringComparer.Ordinal))
        {
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + scopeName);
        }

        descriptor.AddAudiencePermissions(McpResourceIdentifiers.GetAudienceValues(mcpOptions, authorizationServerOptions));
        descriptor.AddResourcePermissions(McpResourceIdentifiers.GetResourceValues(mcpOptions, authorizationServerOptions));

        return descriptor;
    }

    public static bool IsSupportedLoopbackRedirectUri(Uri redirectUri)
    {
        if (!redirectUri.IsAbsoluteUri || !string.Equals(redirectUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return redirectUri.IsLoopback
            || string.Equals(redirectUri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryNormalizeRedirectUri(string redirectUri, out Uri normalizedUri)
    {
        normalizedUri = null!;

        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
        {
            return false;
        }

        normalizedUri = NormalizeRedirectUri(uri);
        return true;
    }

    public static Uri NormalizeRedirectUri(Uri redirectUri)
    {
        if (!IsSupportedLoopbackRedirectUri(redirectUri))
        {
            return redirectUri;
        }

        var builder = new UriBuilder(redirectUri)
        {
            Port = -1
        };

        return builder.Uri;
    }
}
