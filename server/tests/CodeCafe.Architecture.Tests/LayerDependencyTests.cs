using CodeCafe.Application.Ai;
using CodeCafe.Application.Common;
using CodeCafe.Domain.Common;
using CodeCafe.Infrastructure.Persistence;
using System.Reflection;

namespace CodeCafe.Architecture.Tests;

/// <summary>
/// Layer direction, asserted on assembly references. These four invariants are the ones the project
/// structure is meant to guarantee: each inner layer must be compilable without the layers outside it.
/// </summary>
/// <remarks>
/// Replaces the module-oriented assertions in DependencyDirectionTests. Those compared assemblies that
/// no longer exist separately, so several of them had become tautological. Feature-boundary checks that
/// can no longer be expressed as assembly references live in SliceBoundaryTests.
/// </remarks>
public sealed class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(IAuditableEntity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(AiFlowError).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(ApplicationDbContext).Assembly;

    [Fact]
    public void Domain_ReferencesNothingOfOurs()
    {
        // The innermost layer: no CodeCafe reference at all, and no framework beyond the BCL.
        var offenders = GetReferenceNames(DomainAssembly)
            .Where(name => name.StartsWith("CodeCafe", StringComparison.Ordinal))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "CodeCafe.Domain must not reference any other CodeCafe assembly, but references: "
            + string.Join(", ", offenders));
    }

    [Fact]
    public void Domain_DoesNotReferenceEntityFrameworkOrAspNetCore()
    {
        AssertNoReferenceStartingWith(DomainAssembly, "Microsoft.EntityFrameworkCore");
        AssertNoReferenceStartingWith(DomainAssembly, "Microsoft.AspNetCore");
    }

    [Fact]
    public void Application_ReferencesOnlyDomain()
    {
        var offenders = GetReferenceNames(ApplicationAssembly)
            .Where(name => name.StartsWith("CodeCafe", StringComparison.Ordinal))
            .Where(name => name != "CodeCafe.Domain")
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "CodeCafe.Application may only reference CodeCafe.Domain, but also references: "
            + string.Join(", ", offenders));
    }

    [Fact]
    public void Application_DoesNotReferenceAspNetCoreOrEntityFramework()
    {
        // This is what keeps HTTP status codes and DbContext out of use cases. It is also the rule that
        // surfaced AiFlowError carrying an int StatusCode and handlers catching the OpenAI SDK's
        // ClientResultException directly.
        AssertNoReferenceStartingWith(ApplicationAssembly, "Microsoft.AspNetCore");
        AssertNoReferenceStartingWith(ApplicationAssembly, "Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Application_DoesNotReferenceProviderSdks()
    {
        // Provider choice is an Infrastructure concern; a use case naming OpenAI or Markdig types would
        // mean swapping either one requires editing use cases.
        AssertNoReferenceStartingWith(ApplicationAssembly, "OpenAI");
        AssertNoReferenceStartingWith(ApplicationAssembly, "Markdig");
        AssertNoReferenceStartingWith(ApplicationAssembly, "Npgsql");
    }

    [Fact]
    public void Infrastructure_ReferencesOnlyDomainAndApplication()
    {
        var allowed = new[] { "CodeCafe.Domain", "CodeCafe.Application" };
        var offenders = GetReferenceNames(InfrastructureAssembly)
            .Where(name => name.StartsWith("CodeCafe", StringComparison.Ordinal))
            .Where(name => !allowed.Contains(name, StringComparer.Ordinal))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "CodeCafe.Infrastructure may only reference Domain and Application, but also references: "
            + string.Join(", ", offenders));
    }

    [Fact]
    public void Infrastructure_IsNotReferencedBy_DomainOrApplication()
    {
        Assert.DoesNotContain("CodeCafe.Infrastructure", GetReferenceNames(DomainAssembly));
        Assert.DoesNotContain("CodeCafe.Infrastructure", GetReferenceNames(ApplicationAssembly));
    }

    private static void AssertNoReferenceStartingWith(Assembly assembly, string prefix)
    {
        // Prefix rather than exact match: an exact-name check missed a reference to
        // "CodeCafe.Host.Mcp.Domain" while looking for "CodeCafe.Host.Mcp", so two real
        // violations went unnoticed.
        var offenders = GetReferenceNames(assembly)
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"{assembly.GetName().Name} must not reference {prefix}*, but references: "
            + string.Join(", ", offenders));
    }

    private static IReadOnlySet<string> GetReferenceNames(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
    }
}
