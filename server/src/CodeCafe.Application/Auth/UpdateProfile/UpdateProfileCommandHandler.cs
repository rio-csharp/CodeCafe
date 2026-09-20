using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Abstractions;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;

namespace CodeCafe.Application.Auth.UpdateProfile;

public sealed class UpdateProfileCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository users,
    IUnitOfWork unitOfWork
) : ICommandHandler<UpdateProfileCommand, Result<AuthUserDto>>
{
    public async Task<Result<AuthUserDto>> Handle(UpdateProfileCommand message, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        var user = userId is not null
            ? await users.FindByIdAsync(userId.Value, cancellationToken)
            : null;

        if (user is null)
        {
            return Result.Failure<AuthUserDto>(AuthErrors.UserNotFound);
        }

        user.ChangeDisplayName(AuthInput.NormalizeDisplayName(message.DisplayName));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthUserDto(user.Id, user.Email, user.DisplayName));
    }
}
