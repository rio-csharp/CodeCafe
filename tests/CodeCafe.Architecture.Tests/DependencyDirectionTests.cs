using CodeCafe.Api.Common;
using CodeCafe.Ai.Common;
using CodeCafe.Domain.Common.Interfaces;
using CodeCafe.Mcp.Common;
using CodeCafe.Server.Common;
using System.Reflection;

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
        Assert.DoesNotContain("CodeCafe.Ai", references);
        Assert.DoesNotContain("CodeCafe.Mcp", references);
    }

    [Fact]
    public void Application_DoesNotReference_Adapters_Or_Infrastructure()
    {
        var references = GetReferenceNames(typeof(CodeCafe.Application.DependencyInjection).Assembly);

        Assert.DoesNotContain("CodeCafe.Infrastructure", references);
        Assert.DoesNotContain("CodeCafe.WebApi", references);
        Assert.DoesNotContain("CodeCafe.Api", references);
        Assert.DoesNotContain("CodeCafe.Ai", references);
        Assert.DoesNotContain("CodeCafe.Mcp", references);
    }

    [Fact]
    public void Infrastructure_DoesNotReference_Adapters()
    {
        var references = GetReferenceNames(typeof(CodeCafe.Infrastructure.DependencyInjection).Assembly);

        Assert.DoesNotContain("CodeCafe.WebApi", references);
        Assert.DoesNotContain("CodeCafe.Api", references);
        Assert.DoesNotContain("CodeCafe.Ai", references);
        Assert.DoesNotContain("CodeCafe.Mcp", references);
    }

    [Fact]
    public void Adapters_DoNotReference_EachOther()
    {
        var apiReferences = GetReferenceNames(typeof(ApiAssemblyMarker).Assembly);
        var aiReferences = GetReferenceNames(typeof(AiAssemblyMarker).Assembly);
        var mcpReferences = GetReferenceNames(typeof(McpAssemblyMarker).Assembly);

        Assert.DoesNotContain("CodeCafe.Ai", apiReferences);
        Assert.DoesNotContain("CodeCafe.Mcp", apiReferences);
        Assert.DoesNotContain("CodeCafe.Api", aiReferences);
        Assert.DoesNotContain("CodeCafe.Mcp", aiReferences);
        Assert.DoesNotContain("CodeCafe.Api", mcpReferences);
        Assert.DoesNotContain("CodeCafe.Ai", mcpReferences);
    }

    [Fact]
    public void Server_Composes_Adapters_Without_Leaking_Back_Into_Core()
    {
        var serverReferences = GetReferenceNames(typeof(ServerAssemblyMarker).Assembly);

        Assert.Contains("CodeCafe.Api", serverReferences);
        Assert.Contains("CodeCafe.Ai", serverReferences);
        Assert.Contains("CodeCafe.Mcp", serverReferences);
        Assert.Contains("CodeCafe.Application", serverReferences);
        Assert.Contains("CodeCafe.Infrastructure", serverReferences);
        Assert.DoesNotContain("CodeCafe.WebApi", serverReferences);
    }

    private static IReadOnlySet<string> GetReferenceNames(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
    }
}
