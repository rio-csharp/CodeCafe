namespace CodeCafe.Server.Configuration;

public sealed class OpenIddictPruningOptions
{
    public const string SectionName = "OpenIddictPruning";

    // How often expired tokens/authorizations are pruned.
    public int IntervalHours { get; set; } = 24;

    // Only tokens/authorizations that became prunable before this age are
    // removed. Refresh tokens live 7 days, so 14 days keeps recently expired
    // rows available for diagnostics while still bounding table growth.
    public int PruneThresholdDays { get; set; } = 14;
}
