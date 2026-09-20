using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;

namespace CodeCafe.Application.Auth.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher
) : ICommandHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand message, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        var user = userId is not null
            ? await users.FindByIdAsync(userId.Value, cancellationToken)
            : null;

        if (user is null)
        {
            return Result.Failure(AuthErrors.UserNotFound);
        }

        if (!passwordHasher.Verify(message.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure(AuthErrors.IncorrectCurrentPassword);
        }

        user.ChangePasswordHash(passwordHasher.Hash(message.NewPassword));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
