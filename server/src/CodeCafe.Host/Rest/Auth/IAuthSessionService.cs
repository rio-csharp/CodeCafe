namespace CodeCafe.Host.Rest.Auth;

public interface IAuthSessionService
{
    Task SignInAsync(Guid userId, bool isPersistent);

    Task SignOutAsync();
}
