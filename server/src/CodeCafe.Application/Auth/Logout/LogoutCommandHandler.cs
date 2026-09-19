using CodeCafe.Application.Auth.Logout;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Auth.Logout;

public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand, Result>
{
    public Task<Result> Handle(LogoutCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
