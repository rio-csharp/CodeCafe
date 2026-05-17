using Serilog;
using Serilog.Events;

namespace CodeCafe.WebApi.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddCodeCafeSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .WriteTo.Console();
        });

        return builder;
    }
}
