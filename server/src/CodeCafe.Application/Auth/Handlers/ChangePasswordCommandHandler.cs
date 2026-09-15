using CodeCafe.Application.Auth.Commands;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Auth.Handlers;

public sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, Result>
{
    public Task<Result> Handle(ChangePasswordCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
