using CodeCafe.Application.Common.Interfaces;
using CodeCafe.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }
}
