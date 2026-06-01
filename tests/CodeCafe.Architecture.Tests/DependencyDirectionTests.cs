using System.Reflection;
using CodeCafe.Api.Common;
using CodeCafe.Domain.Common.Interfaces;
using CodeCafe.Mcp.Common;

namespace CodeCafe.Architecture.Tests;

public sealed class DependencyDirectionTests
{
    [Fact]
    public void Domain_DoesNotReference_OuterLayers()
    {
        var references = GetReferenceNames(typeof(IAuditableEntity).Assembly);

        Assert.DoesNotContain("CodeCafe.Application", references);
        Assert.DoesNotContain("CodeCafe.Infrastructure", references);
        Assert.DoesNotContain("CodeCafe.WebApi", references);
        Assert.DoesNotContain("CodeCafe.Api", references);
        Assert.DoesNotContain("CodeCafe.Mcp", references);
    }

    [Fact]
    public void Application_DoesNotReference_Adapters_Or_Infrastructure()
    {
        var references = GetReferenceNames(typeof(CodeCafe.Application.DependencyInjection).Assembly);

        Assert.DoesNotContain("CodeCafe.Infrastructure", references);
        Assert.DoesNotContain("CodeCafe.WebApi", references);
        Assert.DoesNotContain("CodeCafe.Api", references);
        Assert.DoesNotContain("CodeCafe.Mcp", references);
    }

    [Fact]
    public void Infrastructure_DoesNotReference_Adapters()
    {
        var references = GetReferenceNames(typeof(CodeCafe.Infrastructure.DependencyInjection).Assembly);

        Assert.DoesNotContain("CodeCafe.WebApi", references);
        Assert.DoesNotContain("CodeCafe.Api", references);
        Assert.DoesNotContain("CodeCafe.Mcp", references);
    }

    [Fact]
    public void Api_And_Mcp_DoNotReference_EachOther()
    {
        var apiReferences = GetReferenceNames(typeof(ApiAssemblyMarker).Assembly);
        var mcpReferences = GetReferenceNames(typeof(McpAssemblyMarker).Assembly);

        Assert.DoesNotContain("CodeCafe.Mcp", apiReferences);
        Assert.DoesNotContain("CodeCafe.Api", mcpReferences);
    }

    private static IReadOnlySet<string> GetReferenceNames(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
    }
}
