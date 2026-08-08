using CodeCafe.Application.Notes;

namespace CodeCafe.Server.DependencyInjection;

/// <summary>
/// Fails composition at startup when a contract declared by one module is only ever implemented by
/// another. <see cref="IMcpContentImportService"/> and <see cref="IMcpUploadStore"/> are declared in
/// Notes.Application and consumed by the Notes markdown-import endpoints, but registered solely by
/// <c>AddCodeCafeMcp</c>. Dropping the MCP module would therefore leave those HTTP endpoints throwing
/// on their first request instead of failing here, where the cause is obvious.
/// </summary>
internal static class CrossModuleContractGuard
{
    public static IServiceCollection AddCodeCafeCrossModuleContractGuard(this IServiceCollection services)
    {
        RequireRegistration(services, typeof(IMcpContentImportService));
        RequireRegistration(services, typeof(IMcpUploadStore));
        return services;
    }

    private static void RequireRegistration(IServiceCollection services, Type serviceType)
    {
        if (services.Any(descriptor => descriptor.ServiceType == serviceType))
        {
            return;
        }

        throw new InvalidOperationException(
            $"{serviceType.Name} has no registration. The Notes markdown-import endpoints depend on it, "
            + "and it is currently provided by AddCodeCafeMcp. Register an implementation before "
            + "AddCodeCafeServerHost, or move the contract's implementation into a Notes-owned component.");
    }
}
