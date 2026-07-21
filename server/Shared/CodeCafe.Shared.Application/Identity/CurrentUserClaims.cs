using System.Security.Claims;

namespace CodeCafe.Shared.Application.Identity;

public static class CurrentUserClaims
{
    public static Guid? GetUserId(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var claimValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(claimValue, out var userId)
            ? userId
            : null;
    }
}
