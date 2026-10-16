using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Domain.Identity;

namespace CodeCafe.Application.Auth.Register;

public sealed class RegisterCommandHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IAccessTokenService accessTokens,
    IRefreshTokenService refreshTokens
) : ICommandHandler<RegisterCommand, Result<AuthSessionDto>>
{
    public async Task<Result<AuthSessionDto>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        // Checked before the email lookup: with sign-up off the endpoint must not become an
        // oracle for which emails are already registered.
        if (!command.RegistrationEnabled)
        {
            return Result.Failure<AuthSessionDto>(AuthErrors.RegistrationDisabled);
        }

        var normalizedEmail = AuthInput.NormalizeEmail(command.Email);
        if (await users.FindByEmailAsync(normalizedEmail, cancellationToken) is not null)
        {
            return Result.Failure<AuthSessionDto>(AuthErrors.EmailAlreadyRegistered);
        }

        var user = User.Create(
            command.Email.Trim(),
            normalizedEmail,
            AuthInput.NormalizeDisplayName(command.DisplayName),
            passwordHasher.Hash(command.Password)
        );

        await users.AddAsync(user, cancellationToken);

        var accessToken = accessTokens.Issue(user.Id);
        var refreshToken = await refreshTokens.IssueAsync(user.Id, cancellationToken);

        // One commit for the whole use case: the user and their refresh token land together.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(AuthSessionDto.From(user, accessToken, refreshToken));
    }
}
