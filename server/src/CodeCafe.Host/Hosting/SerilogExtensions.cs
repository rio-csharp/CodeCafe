using Serilog;

namespace CodeCafe.Host.Hosting;

internal static class SerilogExtensions
{
    public static IHostBuilder UseCodeCafeSerilog(this ConfigureHostBuilder host)
        => host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));
}
