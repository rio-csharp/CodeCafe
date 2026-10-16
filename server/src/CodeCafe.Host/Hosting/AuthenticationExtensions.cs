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
