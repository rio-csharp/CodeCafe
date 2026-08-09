using CodeCafe.Infrastructure.Mcp;
using CodeCafe.Application.Mcp;
using CodeCafe.Application.Notes;
using CodeCafe.Host.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CodeCafe.Host.Tests;

/// <summary>
/// The Notes markdown-import endpoints consume contracts that only the MCP module registers. Without
/// this guard, removing that module turns into a request-time failure on an unrelated-looking HTTP
/// endpoint instead of a startup error naming the missing registration.
/// </summary>
public sealed class CrossModuleContractGuardTests
{
    [Fact]
    public void Guard_Throws_When_ContentImportService_Is_Not_Registered()
    {
        var services = new ServiceCollection();
        services.AddScoped<IMcpUploadStore>(_ => throw new NotSupportedException());

        var exception = Assert.Throws<InvalidOperationException>(
            services.AddCodeCafeCrossModuleContractGuard);

        Assert.Contains(nameof(IMcpContentImportService), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Guard_Throws_When_UploadStore_Is_Not_Registered()
    {
        var services = new ServiceCollection();
        services.AddScoped<IMcpContentImportService>(_ => throw new NotSupportedException());

        var exception = Assert.Throws<InvalidOperationException>(
            services.AddCodeCafeCrossModuleContractGuard);

        Assert.Contains(nameof(IMcpUploadStore), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Guard_Passes_When_Both_Contracts_Are_Registered()
    {
        var services = new ServiceCollection();
        services.AddScoped<IMcpContentImportService>(_ => throw new NotSupportedException());
        services.AddScoped<IMcpUploadStore>(_ => throw new NotSupportedException());

        // The guard inspects registrations only; the factories above are never invoked.
        services.AddCodeCafeCrossModuleContractGuard();
    }
}
