using CodeCafe.Shared.Application.Configuration;
using CodeCafe.Modules.Identity.Presentation.Configuration;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace CodeCafe.Modules.Identity.Presentation.Auth;

public static class OpenIddictClientRegistration
{
    private const string DynamicClientIdPrefix = "codecafe-";
    private const string InvalidAllowedScopesMessage = "Allowed scopes must contain at least one configured MCP read/write scope and no unsupported scopes.";
    private static readonly string[] DefaultProtocolScopes =
    [
        Scopes.OpenId,
        Scopes.Profile,
        Scopes.Email,
        Scopes.OfflineAccess
    ];

    public static string CreateDynamicClientId() => $"{DynamicClientIdPrefix}{Guid.NewGuid():N}";

    public static bool IsDynamicallyRegisteredClientId(string? clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId)
            || !clientId.StartsWith(DynamicClientIdPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var suffix = clientId[DynamicClientIdPrefix.Length..];
        return suffix.Length == 32 && Guid.TryParseExact(suffix, "N", out _);
    }

    public static OpenIddictApplicationDescriptor CreatePublicNativeDescriptor(
        string clientId,
        string? displayName,
        IEnumerable<string> redirectUris,
        IEnumerable<string> allowedScopes,
        McpOptions mcpOptions,
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

        foreach (var scopeName in GetGrantedScopes(allowedScopes, mcpOptions))
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

        return redirectUri;
    }

    public static bool ReconcileDescriptor(
        OpenIddictApplicationDescriptor existingDescriptor,
        OpenIddictApplicationDescriptor desiredDescriptor)
    {
        var changed = false;

        if (!string.Equals(existingDescriptor.DisplayName, desiredDescriptor.DisplayName, StringComparison.Ordinal))
        {
            existingDescriptor.DisplayName = desiredDescriptor.DisplayName;
            changed = true;
        }

        if (!string.Equals(existingDescriptor.ApplicationType, desiredDescriptor.ApplicationType, StringComparison.Ordinal))
        {
            existingDescriptor.ApplicationType = desiredDescriptor.ApplicationType;
            changed = true;
        }

        if (!string.Equals(existingDescriptor.ClientType, desiredDescriptor.ClientType, StringComparison.Ordinal))
        {
            existingDescriptor.ClientType = desiredDescriptor.ClientType;
            changed = true;
        }

        if (!string.Equals(existingDescriptor.ConsentType, desiredDescriptor.ConsentType, StringComparison.Ordinal))
        {
            existingDescriptor.ConsentType = desiredDescriptor.ConsentType;
            changed = true;
        }

        changed |= ReplaceUris(existingDescriptor.RedirectUris, desiredDescriptor.RedirectUris);
        changed |= ReplaceStrings(existingDescriptor.Permissions, desiredDescriptor.Permissions);
        return changed;
    }

    public static bool AreAllowedScopesValid(IEnumerable<string> allowedScopes, McpOptions mcpOptions)
    {
        return TryNormalizeAllowedScopes(allowedScopes, mcpOptions, out _);
    }

    public static IReadOnlyList<string> NormalizeAllowedScopes(IEnumerable<string> allowedScopes, McpOptions mcpOptions)
    {
        return TryNormalizeAllowedScopes(allowedScopes, mcpOptions, out var normalizedScopes)
            ? normalizedScopes
            : throw new InvalidOperationException(InvalidAllowedScopesMessage);
    }

    public static IReadOnlyList<string> GetDynamicClientAllowedScopes(McpOptions mcpOptions)
    {
        return mcpOptions.RequiredReadScopes
            .Concat(mcpOptions.RequiredWriteScopes)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool TryNormalizeAllowedScopes(
        IEnumerable<string> allowedScopes,
        McpOptions mcpOptions,
        out IReadOnlyList<string> normalizedScopes)
    {
        var protectedScopes = mcpOptions.RequiredReadScopes
            .Concat(mcpOptions.RequiredWriteScopes)
            .ToHashSet(StringComparer.Ordinal);

        var supportedScopes = protectedScopes
            .Concat(DefaultProtocolScopes)
            .ToHashSet(StringComparer.Ordinal);

        var requestedScopes = allowedScopes
            .Select(scope => scope?.Trim())
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        normalizedScopes = requestedScopes;
        return requestedScopes.Length > 0
            && requestedScopes.Any(protectedScopes.Contains)
            && requestedScopes.All(supportedScopes.Contains);
    }

    private static IReadOnlyList<string> GetGrantedScopes(
        IEnumerable<string> allowedScopes,
        McpOptions mcpOptions)
    {
        return NormalizeAllowedScopes(allowedScopes, mcpOptions)
            .Concat(DefaultProtocolScopes)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool ReplaceUris(ICollection<Uri> existingValues, IEnumerable<Uri> desiredValues)
    {
        var desiredArray = desiredValues.ToArray();
        var existing = existingValues.Select(uri => uri.AbsoluteUri).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var desired = desiredArray.Select(uri => uri.AbsoluteUri).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (existing.SequenceEqual(desired, StringComparer.Ordinal))
        {
            return false;
        }

        existingValues.Clear();
        foreach (var value in desiredArray)
        {
            existingValues.Add(value);
        }

        return true;
    }

    private static bool ReplaceStrings(ICollection<string> existingValues, IEnumerable<string> desiredValues)
    {
        var desiredArray = desiredValues.Distinct(StringComparer.Ordinal).ToArray();
        var existing = existingValues.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var desired = desiredArray.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (existing.SequenceEqual(desired, StringComparer.Ordinal))
        {
            return false;
        }

        existingValues.Clear();
        foreach (var value in desiredArray)
        {
            existingValues.Add(value);
        }

        return true;
    }
}
