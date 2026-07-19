using System.Net;

namespace CodeCafe.Modules.Identity.Presentation.Networking;

public static class TrustedProxyNetworks
{
    // Cloudflare IPv4 ranges. Source: https://www.cloudflare.com/ips-v4/
    // Last verified: 2026-05-17. Re-verify annually or when seeing
    // legitimate inbound IPs that aren't covered.
    private static readonly string[] CloudflareV4 =
    [
        "173.245.48.0/20",
        "103.21.244.0/22",
        "103.22.200.0/22",
        "103.31.4.0/22",
        "141.101.64.0/18",
        "108.162.192.0/18",
        "190.93.240.0/20",
        "188.114.96.0/20",
        "197.234.240.0/22",
        "198.41.128.0/17",
        "162.158.0.0/15",
        "104.16.0.0/13",
        "104.24.0.0/14",
        "172.64.0.0/13",
        "131.0.72.0/22",
    ];

    // Cloudflare IPv6 ranges. Source: https://www.cloudflare.com/ips-v6/
    private static readonly string[] CloudflareV6 =
    [
        "2400:cb00::/32",
        "2606:4700::/32",
        "2803:f800::/32",
        "2405:b500::/32",
        "2405:8100::/32",
        "2a06:98c0::/29",
        "2c0f:f248::/32",
    ];

    // Loopback + RFC1918 + link-local. Covers in-cluster pod traffic on
    // typical Kubernetes pod CIDRs and local development.
    private static readonly string[] InternalNetworks =
    [
        "127.0.0.0/8",
        "::1/128",
        "10.0.0.0/8",
        "172.16.0.0/12",
        "192.168.0.0/16",
        "169.254.0.0/16",
        "fe80::/10",
    ];

    public static IEnumerable<IPNetwork> All =>
        CloudflareV4
            .Concat(CloudflareV6)
            .Concat(InternalNetworks)
            .Select(IPNetwork.Parse);
}
