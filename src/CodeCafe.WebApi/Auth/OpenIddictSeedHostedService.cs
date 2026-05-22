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

        await EnsureScopeAsync(scopeManager, mcpOptions.RequiredReadScopes, mcpOptions.RequiredAudience, cancellationToken);
        await EnsureScopeAsync(scopeManager, mcpOptions.RequiredWriteScopes, mcpOptions.RequiredAudience, cancellationToken);

        foreach (var client in authorizationServerOptions.PublicClients)
        {
            if (await applicationManager.FindByClientIdAsync(client.ClientId, cancellationToken) is not null)
            {
                continue;
            }

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

            await applicationManager.CreateAsync(descriptor, cancellationToken);

            logger.LogInformation(
                "Seeded OpenIddict client application. ClientId={ClientId}; RedirectUris={RedirectUris}",
                descriptor.ClientId,
                string.Join(", ", descriptor.RedirectUris.Select(uri => uri.AbsoluteUri)));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureScopeAsync(
        IOpenIddictScopeManager scopeManager,
        IEnumerable<string> scopeNames,
        string resource,
        CancellationToken cancellationToken)
    {
        foreach (var scopeName in scopeNames.Distinct(StringComparer.Ordinal))
        {
            if (await scopeManager.FindByNameAsync(scopeName, cancellationToken) is not null)
            {
                continue;
            }

            var descriptor = new OpenIddictScopeDescriptor
            {
                DisplayName = scopeName,
                Name = scopeName
            };
            descriptor.Resources.Add(resource);

            await scopeManager.CreateAsync(descriptor, cancellationToken);
        }
    }
}
