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
        var resolved = await CurrentUserResolver.RequireAsync(currentUserAccessor, users, cancellationToken);
        if (resolved.Error is { } error)
        {
            return Result.Failure<AuthUserDto>(error);
        }

        var user = resolved.Value!;

        return Result.Success(new AuthUserDto(user.Id, user.Email, user.DisplayName));
    }
}
