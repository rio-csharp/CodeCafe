using System.Net;

namespace CodeCafe.WebApi.Networking;

public sealed class ClientIpAddressAccessor : IClientIpAddressAccessor
{
    private static readonly string[] ForwardedIpHeaders =
    [
        "CF-Connecting-IP",
        "True-Client-IP",
        "X-Forwarded-For"
    ];

    public string GetClientIpAddress(HttpContext httpContext)
    {
        foreach (var header in ForwardedIpHeaders)
        {
            var value = httpContext.Request.Headers[header].ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var candidate = header == "X-Forwarded-For"
                ? value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                : value.Trim();

            if (IPAddress.TryParse(candidate, out var ipAddress))
            {
                return ipAddress.ToString();
            }
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
