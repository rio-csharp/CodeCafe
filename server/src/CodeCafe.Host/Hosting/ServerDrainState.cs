namespace CodeCafe.Host.Hosting;

// One-way on purpose: once the process is draining it never reports ready again, so the readiness
// probe can read this without a lock and no path can accidentally re-open traffic.
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

        logger.LogInformation("Server is draining and reports itself unready. Reason={Reason}", reason);
        return true;
    }
}
