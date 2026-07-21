using CodeCafe.Modules.Identity.Presentation.Configuration;
using CodeCafe.Shared.Application.Configuration;

namespace CodeCafe.Modules.Identity.Presentation.Auth;

public static class McpResourceIdentifiers
{
    public static string[] GetAudienceValues(McpOptions mcpOptions, AuthorizationServerOptions authorizationServerOptions)
    {
        var audiences = new HashSet<string>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(mcpOptions.RequiredAudience))
        {
            audiences.Add(mcpOptions.RequiredAudience);
        }

        foreach (var resource in GetResourceValues(mcpOptions, authorizationServerOptions))
        {
            audiences.Add(resource);
        }

        return audiences.ToArray();
    }

    public static string[] GetResourceValues(McpOptions mcpOptions, AuthorizationServerOptions authorizationServerOptions)
    {
        var resources = new HashSet<string>(StringComparer.Ordinal);

        if (Uri.TryCreate(authorizationServerOptions.Issuer, UriKind.Absolute, out var issuer))
        {
            resources.Add(new Uri(issuer, mcpOptions.EndpointPath).AbsoluteUri);
        }

        if (Uri.TryCreate(mcpOptions.RequiredAudience, UriKind.Absolute, out var configuredResource))
        {
            resources.Add(configuredResource.AbsoluteUri);
        }

        return resources.ToArray();
    }
}
