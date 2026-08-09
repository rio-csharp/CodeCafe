namespace CodeCafe.Host.Common;

public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    // CIDR ranges whose X-Forwarded-For headers are trusted. Defaults to
    // loopback only, which keeps local development (same-host proxying)
    // working while ignoring client-supplied X-Forwarded-For headers from
    // anywhere else. Deployments behind a reverse proxy or ingress controller
    // must list its egress networks here; trusting broader ranges lets
    // clients spoof X-Forwarded-For and bypass IP-partitioned rate limits.
    public string[] KnownNetworks { get; set; } = ["127.0.0.0/8", "::1/128"];

    // Single-proxy topology: exactly one forwarding hop is trusted.
    public int ForwardLimit { get; set; } = 1;
}
