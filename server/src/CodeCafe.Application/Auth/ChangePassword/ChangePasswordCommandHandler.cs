using CodeCafe.Application.Auth.ChangePassword;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Auth.ChangePassword;

public sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, Result>
{
    public Task<Result> Handle(ChangePasswordCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
