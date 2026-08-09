using CodeCafe.Host.Rest.Auth;
using CodeCafe.Host.Common;
using CodeCafe.Application.Common.Configuration;
using OpenIddict.Abstractions;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace CodeCafe.Host.Tests;

public sealed class OpenIddictClientRegistrationTests
{
    [Theory]
    [InlineData("codecafe-0123456789abcdef0123456789abcdef", true)]
    [InlineData("codecafe-claude", false)]
    [InlineData("codecafe-01234567-89ab-cdef-0123-456789abcdef", false)]
    [InlineData("other-0123456789abcdef0123456789abcdef", false)]
    [InlineData("", false)]
    public void IsDynamicallyRegisteredClientId_MatchesOnlyGeneratedClientIds(string clientId, bool expected)
    {
        Assert.Equal(expected, OpenIddictClientRegistration.IsDynamicallyRegisteredClientId(clientId));
    }

    [Fact]
    public void NormalizeAllowedScopes_TrimsAndDeduplicatesSupportedScopes()
    {
        var scopes = OpenIddictClientRegistration.NormalizeAllowedScopes(
            [" notes.write ", "notes.read", "notes.write", "openid", ""],
            CreateMcpOptions());

        Assert.Equal(["notes.write", "notes.read", "openid"], scopes);
    }

    [Fact]
    public void NormalizeAllowedScopes_ThrowsWhenScopesAreUnsupportedOrEmpty()
    {
        var mcpOptions = CreateMcpOptions();

        Assert.False(OpenIddictClientRegistration.AreAllowedScopesValid(["notes.delete"], mcpOptions));
        Assert.False(OpenIddictClientRegistration.AreAllowedScopesValid(["openid"], mcpOptions));
        Assert.False(OpenIddictClientRegistration.AreAllowedScopesValid([], mcpOptions));
        Assert.Throws<InvalidOperationException>(() =>
            OpenIddictClientRegistration.NormalizeAllowedScopes(["notes.delete"], mcpOptions));
    }

    [Fact]
    public void CreatePublicNativeDescriptor_AddsStandardProtocolScopes()
    {
        var descriptor = OpenIddictClientRegistration.CreatePublicNativeDescriptor(
            "codecafe-claude",
            "Claude Code",
            ["http://localhost/callback"],
            ["notes.read"],
            CreateMcpOptions(),
            new AuthorizationServerOptions());

        Assert.Contains(Permissions.Prefixes.Scope + Scopes.OpenId, descriptor.Permissions);
        Assert.Contains(Permissions.Prefixes.Scope + Scopes.Profile, descriptor.Permissions);
        Assert.Contains(Permissions.Prefixes.Scope + Scopes.Email, descriptor.Permissions);
        Assert.Contains(Permissions.Prefixes.Scope + Scopes.OfflineAccess, descriptor.Permissions);
        Assert.Contains(Permissions.Prefixes.Scope + "notes.read", descriptor.Permissions);
    }

    [Fact]
    public void GetDynamicClientAllowedScopes_IncludesReadAndWriteScopes()
    {
        var scopes = OpenIddictClientRegistration.GetDynamicClientAllowedScopes(CreateMcpOptions());

        Assert.Equal(["notes.read", "notes.write"], scopes);
    }

    [Fact]
    public void ReconcileDescriptor_RemovesStaleWriteScopeFromDynamicClient()
    {
        var existing = new OpenIddictApplicationDescriptor
        {
            ApplicationType = ApplicationTypes.Native,
            ClientType = ClientTypes.Public,
            ClientId = "codecafe-0123456789abcdef0123456789abcdef",
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "Existing Client"
        };
        existing.RedirectUris.Add(new Uri("http://localhost/callback"));
        existing.Permissions.UnionWith(
        [
            Permissions.Endpoints.Authorization,
            Permissions.Endpoints.Token,
            Permissions.GrantTypes.AuthorizationCode,
            Permissions.GrantTypes.RefreshToken,
            Permissions.ResponseTypes.Code,
            Permissions.Prefixes.Scope + "notes.read",
            Permissions.Prefixes.Scope + "notes.write"
        ]);

        var desired = new OpenIddictApplicationDescriptor
        {
            ApplicationType = ApplicationTypes.Native,
            ClientType = ClientTypes.Public,
            ClientId = existing.ClientId,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = existing.DisplayName
        };
        desired.RedirectUris.Add(new Uri("http://localhost/callback"));
        desired.Permissions.UnionWith(
        [
            Permissions.Endpoints.Authorization,
            Permissions.Endpoints.Token,
            Permissions.GrantTypes.AuthorizationCode,
            Permissions.GrantTypes.RefreshToken,
            Permissions.ResponseTypes.Code,
            Permissions.Prefixes.Scope + "notes.read"
        ]);

        var changed = OpenIddictClientRegistration.ReconcileDescriptor(existing, desired);

        Assert.True(changed);
        Assert.Contains(Permissions.Prefixes.Scope + "notes.read", existing.Permissions);
        Assert.DoesNotContain(Permissions.Prefixes.Scope + "notes.write", existing.Permissions);
    }

    private static McpOptions CreateMcpOptions()
    {
        return new McpOptions
        {
            RequiredReadScopes = ["notes.read"],
            RequiredWriteScopes = ["notes.write"]
        };
    }
}
