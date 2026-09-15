using Microsoft.Extensions.Options;

namespace CodeCafe.Host.Hosting;

internal sealed class ShutdownOptions
{
    public const string SectionName = "Shutdown";

    public int TimeoutSeconds { get; set; } = 30;
}

internal static class ShutdownExtensions
{
    public static IServiceCollection AddCodeCafeShutdown(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ShutdownOptions>()
            .Bind(configuration.GetSection(ShutdownOptions.SectionName))
            .Validate(
                options => options.TimeoutSeconds > 0,
                "Shutdown:TimeoutSeconds must be greater than zero.")
            .ValidateOnStart();

        services.AddOptions<HostOptions>()
            .Configure<IOptions<ShutdownOptions>>((hostOptions, shutdownAccessor) =>
                hostOptions.ShutdownTimeout = TimeSpan.FromSeconds(shutdownAccessor.Value.TimeoutSeconds));

        return services;
    }
}
