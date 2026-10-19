using CodeCafe.Application.Common.Security;
using CodeCafe.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace CodeCafe.Host.Hosting;

internal static class AuthenticationExtensions
{
    public static IServiceCollection AddCodeCafeAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(CodeCafeAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, CodeCafeAuthenticationHandler>(
                CodeCafeAuthenticationHandler.SchemeName,
                _ => { })
            // Separate scheme: only the upload controller opts into it, and it accepts only the
            // short-lived capability prefix, never a PAT or an access token.
            .AddScheme<AuthenticationSchemeOptions, ImportUploadAuthenticationHandler>(
                ImportUploadAuthenticationHandler.SchemeName,
                _ => { });
        // Deny by default: endpoints opt into anonymous access via [AllowAnonymous].
        services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, HttpCurrentUserAccessor>();
        return services;
    }
}
