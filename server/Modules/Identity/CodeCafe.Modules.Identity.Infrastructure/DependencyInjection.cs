using CodeCafe.Modules.Identity.Application.Auth;
using CodeCafe.Modules.Identity.Infrastructure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Modules.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IAuthUserGateway, IdentityAuthUserGateway>();

        return services;
    }
}
