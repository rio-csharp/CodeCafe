using System.Security.Claims;
using CodeCafe.Application.Common.Security;
using Microsoft.AspNetCore.Http;

namespace CodeCafe.Infrastructure.Authentication;

public sealed class HttpCurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public CurrentUser? User
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return subject is not null && Guid.TryParse(subject, out var id)
                ? new CurrentUser(id)
                : null;
        }
    }
}
