using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.Login;

public sealed class LoginCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IAccessTokenService accessTokens,
    IRefreshTokenService refreshTokens
) : ICommandHandler<LoginCommand, Result<AuthSessionDto>>
{
    // Unknown emails verify against a cached dummy hash so the failure path costs
    // the same as a wrong password and timing cannot reveal registered emails.
    private static string? _dummyPasswordHash;

    public async Task<Result<AuthSessionDto>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(AuthInput.NormalizeEmail(command.Email), cancellationToken);

        var passwordHash = user?.PasswordHash ?? (_dummyPasswordHash ??= passwordHasher.Hash("DummyPassword"));
        var passwordVerified = passwordHasher.Verify(command.Password, passwordHash);
        if (user is null || !passwordVerified)
        {
            return Result.Failure<AuthSessionDto>(AuthErrors.InvalidCredentials);
        }

        var accessToken = accessTokens.Issue(user.Id);
        var refreshToken = await refreshTokens.IssueAsync(user.Id, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(
            new AuthSessionDto(
                new AuthUserDto(user.Id, user.Email, user.DisplayName),
                accessToken.Value,
                accessToken.ExpiresAtUtc,
                refreshToken.Value
            )
        );
    }
}
