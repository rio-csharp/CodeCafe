using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Auth.Login;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, Result<AuthSessionDto>>
{
    public Task<Result<AuthSessionDto>> Handle(LoginCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
