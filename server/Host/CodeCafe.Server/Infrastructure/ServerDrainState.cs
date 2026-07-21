namespace CodeCafe.Server.Infrastructure;

public sealed class ServerDrainState(ILogger<ServerDrainState> logger)
{
    private int isDraining;

    public bool IsDraining => Volatile.Read(ref isDraining) == 1;

    public bool BeginDraining(string reason)
    {
        if (Interlocked.Exchange(ref isDraining, 1) == 1)
        {
            return false;
        }

        logger.LogInformation("Server entered draining mode. Reason={Reason}", reason);
        return true;
    }
}
