using CodeCafe.WebApi.Mcp;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace CodeCafe.WebApi.Auth;

public sealed class OpenIddictSeedHostedService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<AuthorizationServerOptions> authorizationServerOptionsAccessor,
    IOptions<McpOptions> mcpOptionsAccessor,
    ILogger<OpenIddictSeedHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        var authorizationServerOptions = authorizationServerOptionsAccessor.Value;
        var mcpOptions = mcpOptionsAccessor.Value;
        var mcpAudienceIdentifiers = McpResourceIdentifiers.GetAudienceValues(mcpOptions, authorizationServerOptions);
        var mcpResourceIdentifiers = McpResourceIdentifiers.GetResourceValues(mcpOptions, authorizationServerOptions);

        await EnsureScopeAsync(scopeManager, mcpOptions.RequiredReadScopes, mcpAudienceIdentifiers, cancellationToken);
        await EnsureScopeAsync(scopeManager, mcpOptions.RequiredWriteScopes, mcpAudienceIdentifiers, cancellationToken);

        foreach (var client in authorizationServerOptions.PublicClients)
        {
            var existingApplication = await applicationManager.FindByClientIdAsync(client.ClientId, cancellationToken);
            if (existingApplication is null)
            {
                var descriptor = CreateApplicationDescriptor(client, mcpOptions, authorizationServerOptions);
                await applicationManager.CreateAsync(descriptor, cancellationToken);

                logger.LogInformation(
                    "Seeded OpenIddict client application. ClientId={ClientId}; RedirectUris={RedirectUris}",
                    descriptor.ClientId,
                    string.Join(", ", descriptor.RedirectUris.Select(uri => uri.AbsoluteUri)));

                continue;
            }

            var existingDescriptor = new OpenIddictApplicationDescriptor();
            await applicationManager.PopulateAsync(existingDescriptor, existingApplication, cancellationToken);
            var desiredDescriptor = CreateApplicationDescriptor(client, mcpOptions, authorizationServerOptions);
            var changed = ReconcileDescriptor(existingDescriptor, desiredDescriptor);

            if (changed)
            {
                await applicationManager.UpdateAsync(existingApplication, existingDescriptor, cancellationToken);

                logger.LogInformation(
                    "Updated OpenIddict client application. ClientId={ClientId}; RedirectUris={RedirectUris}",
                    existingDescriptor.ClientId,
                    string.Join(", ", existingDescriptor.RedirectUris.Select(uri => uri.AbsoluteUri)));
            }
        }
    }

    private static bool ReconcileDescriptor(
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

    private static bool ReplaceUris(
        ICollection<Uri> existingValues,
        IEnumerable<Uri> desiredValues)
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

    private static bool ReplaceStrings(
        ICollection<string> existingValues,
        IEnumerable<string> desiredValues)
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

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureScopeAsync(
        IOpenIddictScopeManager scopeManager,
        IEnumerable<string> scopeNames,
        IReadOnlyCollection<string> resources,
        CancellationToken cancellationToken)
    {
        foreach (var scopeName in scopeNames.Distinct(StringComparer.Ordinal))
        {
            var existingScope = await scopeManager.FindByNameAsync(scopeName, cancellationToken);
            if (existingScope is null)
            {
                var scopeDescriptor = new OpenIddictScopeDescriptor
                {
                    DisplayName = scopeName,
                    Name = scopeName
                };
                foreach (var resource in resources)
                {
                    scopeDescriptor.Resources.Add(resource);
                }

                await scopeManager.CreateAsync(scopeDescriptor, cancellationToken);
                continue;
            }

            var descriptor = new OpenIddictScopeDescriptor();
            await scopeManager.PopulateAsync(descriptor, existingScope, cancellationToken);
            if (ReplaceStrings(descriptor.Resources, resources))
            {
                await scopeManager.UpdateAsync(existingScope, descriptor, cancellationToken);
            }
        }
    }

    private static OpenIddictApplicationDescriptor CreateApplicationDescriptor(
        OAuthClientOptions client,
        McpOptions mcpOptions,
        AuthorizationServerOptions authorizationServerOptions)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ApplicationType = ApplicationTypes.Native,
            ClientType = ClientTypes.Public,
            ClientId = client.ClientId,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = string.IsNullOrWhiteSpace(client.DisplayName) ? client.ClientId : client.DisplayName
        };

        foreach (var redirectUri in client.RedirectUris)
        {
            if (Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
            {
                descriptor.RedirectUris.Add(uri);
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
}
