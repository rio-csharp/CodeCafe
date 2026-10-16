using System.Runtime.InteropServices;

namespace CodeCafe.Host.Hosting;

// SIGUSR1 is a drain request, not a termination request. The chart's preStop hook sends it before
// SIGTERM so the readiness probe fails while the pod is still serving: Kubernetes then stops
// routing to it, and the requests already in flight finish instead of 502-ing. SIGTERM itself
// carries a fixed meaning and SIGKILL cannot be caught, which is why a user-defined signal is the
// one that is free to mean this.
public sealed class ServerDrainHostedService(
    IHostApplicationLifetime applicationLifetime,
    ServerDrainState drainState,
    ILogger<ServerDrainHostedService> logger
) : IHostedService, IDisposable
{
    // PosixSignal has no SIGUSR1 member, so its raw number is used: the documented way to reach a
    // signal the enum does not name is to cast the platform number to PosixSignal. 10 is SIGUSR1 on
    // Linux for x86, ARM and most other architectures, which is what the chart's `kill -USR1`
    // resolves to there; on MIPS and PARISC it would be 16, and those are not deployment targets.
    private const PosixSignal SigUsr1 = (PosixSignal)10;

    private PosixSignalRegistration? registration;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Every shutdown path drains, not just the signal, so the readiness probe never reports a
        // stopping process as ready.
        applicationLifetime.ApplicationStopping.Register(() => drainState.BeginDraining("application_stopping"));

        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            logger.LogDebug("SIGUSR1 draining is not available on this platform.");
            return Task.CompletedTask;
        }

        try
        {
            registration = PosixSignalRegistration.Create(SigUsr1, context =>
            {
                // Without suppressing the default action the runtime would terminate the process,
                // which is the opposite of draining.
                context.Cancel = true;
                drainState.BeginDraining("sigusr1");
            });
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or ArgumentException or IOException)
        {
            // Losing the drain costs a few dropped requests during a rollout; refusing to start
            // would cost an outage, so this stays a warning.
            logger.LogWarning(exception, "SIGUSR1 draining could not be registered.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Dispose() => registration?.Dispose();
}
