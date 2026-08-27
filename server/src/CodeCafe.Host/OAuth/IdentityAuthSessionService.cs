using CodeCafe.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CodeCafe.Host.Rest.Auth;

public sealed class IdentityAuthSessionService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager
) : IAuthSessionService
{
    public async Task SignInAsync(Guid userId, bool isPersistent)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new InvalidOperationException(
                $"User '{userId}' was not found for cookie sign-in."
            );
        }

        await signInManager.SignInAsync(user, isPersistent);
    }

    public Task SignOutAsync() => signInManager.SignOutAsync();
}
