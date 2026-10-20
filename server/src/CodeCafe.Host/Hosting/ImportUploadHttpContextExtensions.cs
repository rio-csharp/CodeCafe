using CodeCafe.Application.Common.Security;

namespace CodeCafe.Host.Hosting;

internal static class ImportUploadHttpContextExtensions
{
    public static ImportUploadGrant? GetImportUploadGrant(this HttpContext context)
        => context.Items[ImportUploadAuthenticationHandler.GrantItemKey] as ImportUploadGrant;
}
