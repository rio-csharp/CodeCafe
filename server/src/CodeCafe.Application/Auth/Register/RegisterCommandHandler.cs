using CodeCafe.Application.Auth.Register;
using CodeCafe.Application.Auth.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Auth.Register;

public sealed class RegisterCommandHandler : ICommandHandler<RegisterCommand, Result<AuthSessionDto>>
{
    public Task<Result<AuthSessionDto>> Handle(RegisterCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
