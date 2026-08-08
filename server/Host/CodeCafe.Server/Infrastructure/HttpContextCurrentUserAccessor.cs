using CodeCafe.Shared.Application.Identity;

namespace CodeCafe.Server.Infrastructure;

public sealed class HttpContextCurrentUserAccessor(
    IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public Guid? GetCurrentUserId()
        => CurrentUserClaims.GetUserId(httpContextAccessor.HttpContext?.User);

    public Guid GetRequiredCurrentUserId()
        => GetCurrentUserId() ?? throw new CurrentUserNotAuthenticatedException();
}
