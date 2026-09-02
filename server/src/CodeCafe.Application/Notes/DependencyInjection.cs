using Microsoft.Extensions.DependencyInjection;

namespace CodeCafe.Application.Notes;

public static class DependencyInjection
{
    public static IServiceCollection AddNotesApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        return services;
    }
}
