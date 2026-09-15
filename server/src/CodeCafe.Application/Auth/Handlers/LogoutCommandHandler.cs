using CodeCafe.Application.Auth.Commands;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Auth.Handlers;

public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand, Result>
{
    public Task<Result> Handle(LogoutCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
