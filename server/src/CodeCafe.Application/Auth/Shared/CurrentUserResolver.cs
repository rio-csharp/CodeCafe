using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Security;
using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Auth.Shared;

// The one place that decides what "the caller no longer exists" means. A valid access token can
// outlive its user when the account was deleted after issuance, so handlers that need the user
// entity (not just the id) resolve it here instead of repeating the null dance.
public static class CurrentUserResolver
{
    public static async Task<Result<User>> RequireAsync(
        ICurrentUserAccessor currentUserAccessor,
        IUserRepository users,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUserAccessor.User?.Id;
        var user = userId is not null
            ? await users.FindByIdAsync(userId.Value, cancellationToken)
            : null;

        return user is null
            ? Result.Failure<User>(AuthErrors.UserNotFound)
            : Result.Success(user);
    }
}
