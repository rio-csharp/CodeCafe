namespace CodeCafe.Application.Common.Identity;

public interface ICurrentUserAccessor
{
    Guid? GetCurrentUserId();

    /// <summary>
    /// Returns the current user's id, or throws <see cref="CurrentUserNotAuthenticatedException"/>
    /// when the request carries no authenticated user id. The host maps that
    /// exception to a 401 <c>authentication_required</c> problem response, so
    /// endpoints that require an actor can use this instead of falling back to
    /// <see cref="Guid.Empty"/> (which would silently attribute writes to an
    /// empty user or produce a 500 further down the pipeline).
    /// </summary>
    Guid GetRequiredCurrentUserId();
}
