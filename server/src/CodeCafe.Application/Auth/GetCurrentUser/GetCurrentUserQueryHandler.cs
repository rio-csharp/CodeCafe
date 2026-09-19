using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Auth.GetCurrentUser;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Auth.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler : IQueryHandler<GetCurrentUserQuery, Result<AuthUserDto>>
{
    public Task<Result<AuthUserDto>> Handle(GetCurrentUserQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
