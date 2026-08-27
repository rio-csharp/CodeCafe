using CodeCafe.Application.Common.Identity;

namespace CodeCafe.Host.Common;

public sealed class HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserAccessor
{
    public Guid? GetCurrentUserId() =>
        CurrentUserClaims.GetUserId(httpContextAccessor.HttpContext?.User);

    public Guid GetRequiredCurrentUserId() =>
        GetCurrentUserId() ?? throw new CurrentUserNotAuthenticatedException();
}
