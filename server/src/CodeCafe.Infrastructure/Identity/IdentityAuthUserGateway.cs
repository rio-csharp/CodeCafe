using CodeCafe.Application.Identity;
using Microsoft.AspNetCore.Identity;

namespace CodeCafe.Infrastructure.Identity;

/// <remarks>
/// The <see cref="CancellationToken"/> parameters exist for interface
/// conformance only: <see cref="UserManager{TUser}"/> exposes no
/// cancellation-token overloads, so cancellation is not honored here.
/// </remarks>
public sealed class IdentityAuthUserGateway(UserManager<ApplicationUser> userManager)
    : IAuthUserGateway
{
    public async Task<AuthUserModel?> FindByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken
    )
    {
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        return ToModel(user);
    }

    public async Task<AuthUserModel?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return ToModel(user);
    }

    public async Task<AuthCreateUserResult> CreateUserAsync(
        string normalizedEmail,
        string displayName,
        string password,
        CancellationToken cancellationToken
    )
    {
        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            DisplayName = displayName,
            EmailConfirmed = false,
        };

        var result = await userManager.CreateAsync(user, password);
        return result.Succeeded
            ? AuthCreateUserResult.Success(ToModel(user)!)
            : AuthCreateUserResult.Failure(result.Errors.Select(error => error.Code).ToArray());
    }

    // Placeholder user for hashing work that is never persisted; the hasher only
    // needs an instance, none of its properties feed into the hash.
    private static ApplicationUser TimingPaddingUser => new() { DisplayName = string.Empty };

    // Precomputed PBKDF2 hash used only to equalize login timing: verifying a
    // password against it costs roughly the same as verifying against a real
    // account's hash, so unknown accounts do not answer measurably faster.
    private static readonly string TimingPaddingPasswordHash =
        new PasswordHasher<ApplicationUser>().HashPassword(TimingPaddingUser, "TimingPadding123!");

    public async Task<AuthPasswordVerificationResult> VerifyPasswordAsync(
        string normalizedEmail,
        string password,
        bool lockoutOnFailure,
        CancellationToken cancellationToken
    )
    {
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            _ = userManager.PasswordHasher.VerifyHashedPassword(
                TimingPaddingUser,
                TimingPaddingPasswordHash,
                password
            );
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

            return AuthPasswordVerificationResult.Success(ToModel(user)!);
        }

        if (lockoutOnFailure && userManager.SupportsUserLockout)
        {
            await userManager.AccessFailedAsync(user);
        }

        // AccessFailedAsync updates LockoutEnd on the tracked entity when the
        // failure threshold is reached, so the lockout state can be read off
        // the entity without querying the store again.
        var isLockedOut =
            userManager.SupportsUserLockout
            && user.LockoutEnd.HasValue
            && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
        return AuthPasswordVerificationResult.Failure(isLockedOut);
    }

    private static AuthUserModel? ToModel(ApplicationUser? user)
    {
        return user is null
            ? null
            : new AuthUserModel(user.Id, user.Email ?? string.Empty, user.DisplayName);
    }
}
