namespace CodeCafe.WebApi.Health;

public sealed class ReadinessState
{
    private volatile bool _isReady;

    public bool IsReady => _isReady;

    public void MarkReady()
    {
        _isReady = true;
    }

    public void MarkNotReady()
    {
        _isReady = false;
    }
}
