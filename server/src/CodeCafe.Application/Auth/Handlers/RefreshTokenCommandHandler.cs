using CodeCafe.Application.Auth.Commands;
using CodeCafe.Application.Auth.Models;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.Handlers;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, Result<AuthSessionDto>>
{
    public Task<Result<AuthSessionDto>> Handle(RefreshTokenCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
