using System.Runtime.InteropServices;
using CodeCafe.Host.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace CodeCafe.Host.Tests.Hosting;

public sealed class ServerDrainTests
{
    [Fact]
    public void BeginDraining_IsOneWay()
    {
        var state = new ServerDrainState(NullLogger<ServerDrainState>.Instance);

        Assert.False(state.IsDraining);

        Assert.True(state.BeginDraining("test"));
        Assert.True(state.IsDraining);

        // A second reason must not reset the flag, or the readiness probe could flip back to
        // healthy while the process is on its way out.
        Assert.False(state.BeginDraining("again"));
        Assert.True(state.IsDraining);
    }

    [Fact]
    public async Task Readiness_FailsWhileDraining()
    {
        var state = new ServerDrainState(NullLogger<ServerDrainState>.Instance);
        var check = new ServerDrainReadinessHealthCheck(state);

        var before = await check.CheckHealthAsync(
            new HealthCheckContext(),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HealthStatus.Healthy, before.Status);

        state.BeginDraining("test");

        var after = await check.CheckHealthAsync(
            new HealthCheckContext(),
            TestContext.Current.CancellationToken
        );
        Assert.Equal(HealthStatus.Unhealthy, after.Status);
    }

    [Fact]
    public async Task HostedService_DrainsWhenSigUsr1Arrives()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "SIGUSR1 is a POSIX signal.");

        var state = new ServerDrainState(NullLogger<ServerDrainState>.Instance);
        using var service = new ServerDrainHostedService(
            new StubApplicationLifetime(),
            state,
            NullLogger<ServerDrainHostedService>.Instance
        );
        await service.StartAsync(CancellationToken.None);

        // Raising rather than killing: if the registration did not suppress the default action the
        // test process would die here, which is exactly the failure this guards against.
        Raise(SigUsr1);

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!state.IsDraining && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        Assert.True(state.IsDraining, "SIGUSR1 did not put the server into drain mode.");
    }

    [Fact]
    public async Task HostedService_DrainsWhenTheApplicationStops()
    {
        var lifetime = new StubApplicationLifetime();
        var state = new ServerDrainState(NullLogger<ServerDrainState>.Instance);
        using var service = new ServerDrainHostedService(
            lifetime,
            state,
            NullLogger<ServerDrainHostedService>.Instance
        );
        await service.StartAsync(CancellationToken.None);

        lifetime.Stop();

        Assert.True(state.IsDraining);
    }

    private const int SigUsr1 = 10;

    [DllImport("libc", EntryPoint = "raise")]
    private static extern int Raise(int signal);

    private sealed class StubApplicationLifetime : IHostApplicationLifetime
    {
        private readonly CancellationTokenSource stopping = new();

        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => stopping.Token;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication() => Stop();

        public void Stop() => stopping.Cancel();
    }
}
