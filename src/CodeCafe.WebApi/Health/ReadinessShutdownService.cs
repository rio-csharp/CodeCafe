namespace CodeCafe.WebApi.Health;

public sealed class ReadinessShutdownService : IHostedLifecycleService
{
    private readonly ReadinessState _readinessState;

    public ReadinessShutdownService(ReadinessState readinessState)
    {
        _readinessState = readinessState;
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StartedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        _readinessState.MarkNotReady();
        return Task.CompletedTask;
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
