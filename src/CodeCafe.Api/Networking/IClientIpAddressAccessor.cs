namespace CodeCafe.Api.Networking;

public interface IClientIpAddressAccessor
{
    string GetClientIpAddress(HttpContext httpContext);
}
