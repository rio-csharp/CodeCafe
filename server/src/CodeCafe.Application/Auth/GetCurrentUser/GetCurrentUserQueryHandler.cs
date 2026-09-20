using CodeCafe.Application.Auth.Abstractions;
using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Common.Security;

namespace CodeCafe.Application.Auth.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository users
) : IQueryHandler<GetCurrentUserQuery, Result<AuthUserDto>>
{
    public async Task<Result<AuthUserDto>> Handle(GetCurrentUserQuery message, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.User?.Id;
        var user = userId is not null
            ? await users.FindByIdAsync(userId.Value, cancellationToken)
            : null;

        if (user is null)
        {
            return Result.Failure<AuthUserDto>(AuthErrors.UserNotFound);
        }

        return Result.Success(new AuthUserDto(user.Id, user.Email, user.DisplayName));
    }
}
