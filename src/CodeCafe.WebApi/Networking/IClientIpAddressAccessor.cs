namespace CodeCafe.WebApi.Networking;

public interface IClientIpAddressAccessor
{
    string GetClientIpAddress(HttpContext httpContext);
}
