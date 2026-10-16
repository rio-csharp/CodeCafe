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
        var resolved = await CurrentUserResolver.RequireAsync(currentUserAccessor, users, cancellationToken);
        if (resolved.Error is { } error)
        {
            return Result.Failure<AuthUserDto>(error);
        }

        var user = resolved.Value!;

        user.ChangeDisplayName(AuthInput.NormalizeDisplayName(message.DisplayName));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthUserDto(user.Id, user.Email, user.DisplayName));
    }
}
