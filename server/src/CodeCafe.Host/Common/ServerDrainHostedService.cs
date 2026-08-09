using System.Runtime.InteropServices;

namespace CodeCafe.Host.Common;

public sealed class ServerDrainHostedService(
    IHostApplicationLifetime hostApplicationLifetime,
    ServerDrainState drainState,
    ILogger<ServerDrainHostedService> logger) : IHostedService, IDisposable
{
    private const PosixSignal SigUsr1 = (PosixSignal)10;
    private PosixSignalRegistration? sigUsr1Registration;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        hostApplicationLifetime.ApplicationStopping.Register(() => drainState.BeginDraining("application_stopping"));

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            try
            {
                sigUsr1Registration = PosixSignalRegistration.Create(SigUsr1, context =>
                {
                    context.Cancel = true;
                    drainState.BeginDraining("sigusr1");
                });
            }
            catch (PlatformNotSupportedException)
            {
                logger.LogDebug("SIGUSR1 draining hook is not supported on this platform.");
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Dispose()
    {
        sigUsr1Registration?.Dispose();
    }
}
