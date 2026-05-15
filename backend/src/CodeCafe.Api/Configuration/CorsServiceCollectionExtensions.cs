namespace CodeCafe.Api.Configuration;

internal static class CorsServiceCollectionExtensions
{
    public static IServiceCollection AddApiCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(
                ApiPolicyNames.FrontendCors,
                policy => policy
                    .WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });

        return services;
    }
}
