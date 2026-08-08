using CodeCafe.Modules.Ai.Common;
using CodeCafe.Modules.Mcp.Common;
using CodeCafe.Modules.Notes.Presentation.Common;
using CodeCafe.Server.Common;
using CodeCafe.Application.Common;
using CodeCafe.Domain.Common;
using CodeCafe.Infrastructure.Persistence;
using System.Reflection;

namespace CodeCafe.Architecture.Tests;

public sealed class DependencyDirectionTests
{
    [Fact]
    public void Domain_DoesNotReference_OuterLayers()
    {
        var references = GetReferenceNames(typeof(IAuditableEntity).Assembly);

        Assert.DoesNotContain("CodeCafe.Application", references);
        Assert.DoesNotContain("CodeCafe.Modules.Notes.Infrastructure", references);
        Assert.DoesNotContain("CodeCafe.Modules.Identity.Presentation", references);
        Assert.DoesNotContain("CodeCafe.Modules.Notes.Presentation", references);
        Assert.DoesNotContain("CodeCafe.Modules.Ai", references);
        Assert.DoesNotContain("CodeCafe.Modules.Mcp", references);
    }

    [Fact]
    public void Application_Modules_DoNotReference_Presentation_Or_Infrastructure()
    {
        var identityReferences = GetReferenceNames(typeof(CodeCafe.Application.Identity.DependencyInjection).Assembly);
        var notesReferences = GetReferenceNames(typeof(CodeCafe.Application.Notes.DependencyInjection).Assembly);

        Assert.DoesNotContain("CodeCafe.Infrastructure", identityReferences);
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
    public void Notes_And_Identity_Slices_DoNotReference_EachOther()
    {
        // Feature slices now share the CodeCafe.Application assembly, so this invariant can no longer
        // be expressed as an assembly reference. It is checked on namespaces instead: a Notes use case
        // must not reach into the Identity slice or vice versa. Cross-slice work belongs in the host.
        AssertSliceDoesNotImport("Notes", "CodeCafe.Application.Identity");
        AssertSliceDoesNotImport("Identity", "CodeCafe.Application.Notes");
    }

    private static void AssertSliceDoesNotImport(string slice, string forbiddenNamespace)
    {
        var sliceDirectory = Path.Combine(FindApplicationProjectDirectory(), slice);
        var offenders = Directory
            .EnumerateFiles(sliceDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(file => File.ReadAllText(file).Contains(forbiddenNamespace, StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"Application/{slice} must not depend on {forbiddenNamespace}, but these files do: "
            + string.Join(", ", offenders));
    }

    private static string FindApplicationProjectDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "server", "src", "CodeCafe.Application");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate CodeCafe.Application from the test output path.");
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
    public void Module_Presentations_DoNotReference_OtherModule_Presentations()
    {
        // Host-wide presentation policy (ApiProblems, GlobalExceptionHandler)
        // lives in Shared.Presentation / the host, so no module Presentation
        // assembly may depend on another module's Presentation assembly.
        var identityPresentationReferences = GetReferenceNames(
            typeof(CodeCafe.Modules.Identity.Presentation.Auth.DynamicClientRegistrationController).Assembly);
        var notesPresentationReferences = GetReferenceNames(typeof(ApiAssemblyMarker).Assembly);

        Assert.DoesNotContain("CodeCafe.Modules.Notes.Presentation", identityPresentationReferences);
        Assert.DoesNotContain("CodeCafe.Modules.Identity.Presentation", notesPresentationReferences);
    }

    [Fact]
    public void Server_Composes_Adapters_Without_Leaking_Back_Into_Core()
    {
        var serverReferences = GetReferenceNames(typeof(ServerAssemblyMarker).Assembly);

        Assert.Contains("CodeCafe.Modules.Identity.Presentation", serverReferences);
        Assert.Contains("CodeCafe.Modules.Notes.Presentation", serverReferences);
        Assert.Contains("CodeCafe.Modules.Ai", serverReferences);
        Assert.Contains("CodeCafe.Modules.Mcp", serverReferences);
        Assert.Contains("CodeCafe.Application", serverReferences);
        Assert.Contains("CodeCafe.Infrastructure", serverReferences);
        Assert.Contains("CodeCafe.Modules.Notes.Infrastructure", serverReferences);
    }

    [Fact]
    public void SharedInfrastructure_IsNotReferenced_By_Domain_Or_Application()
    {
        var referencesByAssembly = new[]
        {
            // McpToolAuditEntry now lives in Shared.Domain alongside IAuditableEntity, so it no
            // longer needs its own entry here.
            GetReferenceNames(typeof(IAuditableEntity).Assembly),
            GetReferenceNames(typeof(CodeCafe.Domain.Notes.Notebook).Assembly),
            GetReferenceNames(typeof(IDateTimeProvider).Assembly),
            GetReferenceNames(typeof(CodeCafe.Application.Identity.DependencyInjection).Assembly),
            GetReferenceNames(typeof(CodeCafe.Application.Notes.DependencyInjection).Assembly)
        };

        foreach (var references in referencesByAssembly)
        {
            Assert.DoesNotContain("CodeCafe.Infrastructure", references);
        }
    }

    [Fact]
    public void SharedInfrastructure_IsReferenced_By_InfrastructureLevel_And_Host()
    {
        Assert.Contains("CodeCafe.Infrastructure", GetReferenceNames(typeof(CodeCafe.Modules.Notes.Infrastructure.DependencyInjection).Assembly));
        Assert.Contains("CodeCafe.Infrastructure", GetReferenceNames(typeof(McpAssemblyMarker).Assembly));
        Assert.Contains("CodeCafe.Infrastructure", GetReferenceNames(typeof(CodeCafe.Modules.Identity.Presentation.Auth.DynamicClientRegistrationController).Assembly));
        Assert.Contains("CodeCafe.Infrastructure", GetReferenceNames(typeof(ServerAssemblyMarker).Assembly));
    }

    [Fact]
    public void Shared_And_Notes_DoNotReference_AnyMcpAssembly()
    {
        // The exact-name assertions elsewhere in this file miss satellite assemblies: a reference to
        // "CodeCafe.Modules.Mcp.Domain" is not equal to "CodeCafe.Modules.Mcp", so Shared.Infrastructure
        // and Notes.Infrastructure both depended on the Mcp module unnoticed. Match by prefix instead.
        AssertNoReferenceStartingWith(typeof(IAuditableEntity).Assembly, "CodeCafe.Modules.Mcp");
        AssertNoReferenceStartingWith(typeof(ApplicationDbContext).Assembly, "CodeCafe.Modules.Mcp");
        AssertNoReferenceStartingWith(
            typeof(CodeCafe.Modules.Notes.Infrastructure.DependencyInjection).Assembly,
            "CodeCafe.Modules.Mcp");
    }

    private static void AssertNoReferenceStartingWith(Assembly assembly, string prefix)
    {
        var offenders = GetReferenceNames(assembly)
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"{assembly.GetName().Name} must not reference {prefix}*, but references: {string.Join(", ", offenders)}");
    }

    private static IReadOnlySet<string> GetReferenceNames(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
    }
}
