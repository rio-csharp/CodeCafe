namespace CodeCafe.Mcp.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCodeCafeMcp(this IServiceCollection services)
    {
        services.AddProblemDetails();
        return services;
    }
}
