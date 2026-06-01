namespace CodeCafe.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodeCafeApi(this IServiceCollection services)
    {
        services.AddProblemDetails();
        return services;
    }
}
