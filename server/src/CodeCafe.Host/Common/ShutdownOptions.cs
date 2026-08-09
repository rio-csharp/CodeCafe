namespace CodeCafe.Host.Common;

public sealed class ShutdownOptions
{
    public const string SectionName = "Shutdown";

    public int TimeoutSeconds { get; set; } = 45;
}
