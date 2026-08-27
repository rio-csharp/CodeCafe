namespace CodeCafe.Application.Common.Identity;

/// <summary>
/// Thrown when an operation requires an authenticated user but the current
/// request carries no user id. The host maps this to a 401 problem response
/// with the <c>authentication_required</c> error code, matching the response
/// the authentication cookie events produce for unauthenticated requests.
/// </summary>
public sealed class CurrentUserNotAuthenticatedException : Exception
{
    public CurrentUserNotAuthenticatedException()
        : base("The current request has no authenticated user.") { }
}
