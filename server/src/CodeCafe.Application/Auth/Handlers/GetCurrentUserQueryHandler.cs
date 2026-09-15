using CodeCafe.Application.Auth.Models;
using CodeCafe.Application.Auth.Queries;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Auth.Handlers;

public sealed class GetCurrentUserQueryHandler : IQueryHandler<GetCurrentUserQuery, Result<AuthUserDto>>
{
    public Task<Result<AuthUserDto>> Handle(GetCurrentUserQuery message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
