namespace CodeCafe.Api.Endpoints.Auth;

public interface IAuthSessionService
{
    Task SignInAsync(Guid userId, bool isPersistent);

    Task SignOutAsync();
}
