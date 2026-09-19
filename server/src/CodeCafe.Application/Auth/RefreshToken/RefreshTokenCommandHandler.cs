using CodeCafe.Application.Auth.RefreshToken;
using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Auth.RefreshToken;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, Result<AuthSessionDto>>
{
    public Task<Result<AuthSessionDto>> Handle(RefreshTokenCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
