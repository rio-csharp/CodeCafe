namespace CodeCafe.Modules.Identity.Presentation.Networking;

public interface IClientIpAddressAccessor
{
    string GetClientIpAddress(HttpContext httpContext);
}
