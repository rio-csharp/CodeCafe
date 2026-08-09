namespace CodeCafe.Host.Common;

public interface IClientIpAddressAccessor
{
    string GetClientIpAddress(HttpContext httpContext);
}
