using CodeCafe.Application.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Infrastructure.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IAuthUserGateway, IdentityAuthUserGateway>();

        return services;
    }
}
