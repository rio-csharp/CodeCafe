using CodeCafe.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace CodeCafe.Infrastructure.Identity;

public sealed class IdentityAuthUserGateway(
    UserManager<ApplicationUser> userManager) : IAuthUserGateway
{
    public async Task<AuthUserModel?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        return ToModel(user);
    }

    public async Task<AuthUserModel?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return ToModel(user);
    }

    public async Task<AuthCreateUserResult> CreateUserAsync(
        string normalizedEmail,
        string displayName,
        string password,
        CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            DisplayName = displayName,
            EmailConfirmed = false
        };

        var result = await userManager.CreateAsync(user, password);
        return result.Succeeded
            ? AuthCreateUserResult.Success(ToModel(user)!)
            : AuthCreateUserResult.Failure(result.Errors.Select(error => error.Code).ToArray());
    }

    public async Task<AuthPasswordVerificationResult> VerifyPasswordAsync(
        Guid userId,
        string password,
        bool lockoutOnFailure,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthPasswordVerificationResult.Failure(isLockedOut: false);
        }

        if (userManager.SupportsUserLockout && await userManager.IsLockedOutAsync(user))
        {
            return AuthPasswordVerificationResult.Failure(isLockedOut: true);
        }

        if (await userManager.CheckPasswordAsync(user, password))
        {
            if (userManager.SupportsUserLockout)
            {
                await userManager.ResetAccessFailedCountAsync(user);
            }

            return AuthPasswordVerificationResult.Success();
        }

        if (lockoutOnFailure && userManager.SupportsUserLockout)
        {
            await userManager.AccessFailedAsync(user);
        }

        var isLockedOut = userManager.SupportsUserLockout && await userManager.IsLockedOutAsync(user);
        return AuthPasswordVerificationResult.Failure(isLockedOut);
    }

    private static AuthUserModel? ToModel(ApplicationUser? user)
    {
        return user is null
            ? null
            : new AuthUserModel(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName);
    }
}
