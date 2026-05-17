namespace CodeCafe.WebApi.Networking;

public sealed class ClientIpAddressAccessor : IClientIpAddressAccessor
{
    public string GetClientIpAddress(HttpContext httpContext)
    {
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
