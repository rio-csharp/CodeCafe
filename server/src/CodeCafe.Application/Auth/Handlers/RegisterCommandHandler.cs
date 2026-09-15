using CodeCafe.Application.Auth.Commands;
using CodeCafe.Application.Auth.Models;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Auth.Handlers;

public sealed class RegisterCommandHandler : ICommandHandler<RegisterCommand, Result<AuthSessionDto>>
{
    public Task<Result<AuthSessionDto>> Handle(RegisterCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
