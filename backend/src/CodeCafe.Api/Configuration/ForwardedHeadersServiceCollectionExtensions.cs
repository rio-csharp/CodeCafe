using Microsoft.AspNetCore.HttpOverrides;

namespace CodeCafe.Api.Configuration;

internal static class ForwardedHeadersServiceCollectionExtensions
{
    public static IServiceCollection AddApiForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }
}
