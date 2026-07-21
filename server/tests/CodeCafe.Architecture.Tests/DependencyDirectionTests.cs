using CodeCafe.Modules.Ai.Common;
using CodeCafe.Modules.Mcp.Common;
using CodeCafe.Modules.Notes.Presentation.Common;
using CodeCafe.Server.Common;
using CodeCafe.Shared.Application.Common.Interfaces;
using CodeCafe.Shared.Domain.Common.Interfaces;
using System.Reflection;

namespace CodeCafe.Architecture.Tests;

public sealed class DependencyDirectionTests
{
    [Fact]
    public void Domain_DoesNotReference_OuterLayers()
    {
        var references = GetReferenceNames(typeof(IAuditableEntity).Assembly);

        Assert.DoesNotContain("CodeCafe.Modules.Identity.Application", references);
        Assert.DoesNotContain("CodeCafe.Modules.Notes.Application", references);
        Assert.DoesNotContain("CodeCafe.Modules.Notes.Infrastructure", references);
        Assert.DoesNotContain("CodeCafe.Modules.Identity.Presentation", references);
        Assert.DoesNotContain("CodeCafe.Modules.Notes.Presentation", references);
        Assert.DoesNotContain("CodeCafe.Modules.Ai", references);
        Assert.DoesNotContain("CodeCafe.Modules.Mcp", references);
    }

    [Fact]
    public void Application_Modules_DoNotReference_Presentation_Or_Infrastructure()
    {
        var identityReferences = GetReferenceNames(typeof(CodeCafe.Modules.Identity.Application.DependencyInjection).Assembly);
        var notesReferences = GetReferenceNames(typeof(CodeCafe.Modules.Notes.Application.DependencyInjection).Assembly);

        Assert.DoesNotContain("CodeCafe.Modules.Identity.Infrastructure", identityReferences);
        Assert.DoesNotContain("CodeCafe.Modules.Identity.Presentation", identityReferences);
        Assert.DoesNotContain("CodeCafe.Modules.Notes.Infrastructure", notesReferences);
        Assert.DoesNotContain("CodeCafe.Modules.Notes.Presentation", notesReferences);
        Assert.DoesNotContain("CodeCafe.Modules.Ai", identityReferences);
        Assert.DoesNotContain("CodeCafe.Modules.Mcp", notesReferences);
    }

    [Fact]
    public void Infrastructure_DoesNotReference_Adapters()
    {
        var references = GetReferenceNames(typeof(CodeCafe.Modules.Notes.Infrastructure.DependencyInjection).Assembly);

        Assert.DoesNotContain("CodeCafe.Modules.Identity.Presentation", references);
        Assert.DoesNotContain("CodeCafe.Modules.Notes.Presentation", references);
        Assert.DoesNotContain("CodeCafe.Modules.Ai", references);
        Assert.DoesNotContain("CodeCafe.Modules.Mcp", references);
    }

    [Fact]
    public void Adapters_DoNotReference_EachOther()
    {
        var apiReferences = GetReferenceNames(typeof(ApiAssemblyMarker).Assembly);
        var aiReferences = GetReferenceNames(typeof(AiAssemblyMarker).Assembly);
        var mcpReferences = GetReferenceNames(typeof(McpAssemblyMarker).Assembly);

        Assert.DoesNotContain("CodeCafe.Modules.Ai", apiReferences);
        Assert.DoesNotContain("CodeCafe.Modules.Mcp", apiReferences);
        Assert.DoesNotContain("CodeCafe.Modules.Notes.Presentation", aiReferences);
        Assert.DoesNotContain("CodeCafe.Modules.Mcp", aiReferences);
        Assert.DoesNotContain("CodeCafe.Modules.Notes.Presentation", mcpReferences);
        Assert.DoesNotContain("CodeCafe.Modules.Ai", mcpReferences);
    }

    [Fact]
    public void Server_Composes_Adapters_Without_Leaking_Back_Into_Core()
    {
        var serverReferences = GetReferenceNames(typeof(ServerAssemblyMarker).Assembly);

        Assert.Contains("CodeCafe.Modules.Identity.Presentation", serverReferences);
        Assert.Contains("CodeCafe.Modules.Notes.Presentation", serverReferences);
        Assert.Contains("CodeCafe.Modules.Ai", serverReferences);
        Assert.Contains("CodeCafe.Modules.Mcp", serverReferences);
        Assert.Contains("CodeCafe.Modules.Identity.Application", serverReferences);
        Assert.Contains("CodeCafe.Modules.Notes.Application", serverReferences);
        Assert.Contains("CodeCafe.Modules.Notes.Infrastructure", serverReferences);
    }

    [Fact]
    public void SharedInfrastructure_IsNotReferenced_By_Domain_Or_Application()
    {
        var referencesByAssembly = new[]
        {
            GetReferenceNames(typeof(IAuditableEntity).Assembly),
            GetReferenceNames(typeof(CodeCafe.Modules.Notes.Domain.Notes.Notebook).Assembly),
            GetReferenceNames(typeof(CodeCafe.Shared.Domain.Mcp.McpToolAuditEntry).Assembly),
            GetReferenceNames(typeof(IDateTimeProvider).Assembly),
            GetReferenceNames(typeof(CodeCafe.Modules.Identity.Application.DependencyInjection).Assembly),
            GetReferenceNames(typeof(CodeCafe.Modules.Notes.Application.DependencyInjection).Assembly)
        };

        foreach (var references in referencesByAssembly)
        {
            Assert.DoesNotContain("CodeCafe.Shared.Infrastructure", references);
        }
    }

    [Fact]
    public void SharedInfrastructure_IsReferenced_By_InfrastructureLevel_And_Host()
    {
        Assert.Contains("CodeCafe.Shared.Infrastructure", GetReferenceNames(typeof(CodeCafe.Modules.Notes.Infrastructure.DependencyInjection).Assembly));
        Assert.Contains("CodeCafe.Shared.Infrastructure", GetReferenceNames(typeof(McpAssemblyMarker).Assembly));
        Assert.Contains("CodeCafe.Shared.Infrastructure", GetReferenceNames(typeof(CodeCafe.Modules.Identity.Presentation.Auth.DynamicClientRegistrationController).Assembly));
        Assert.Contains("CodeCafe.Shared.Infrastructure", GetReferenceNames(typeof(ServerAssemblyMarker).Assembly));
    }

    private static IReadOnlySet<string> GetReferenceNames(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
    }
}
