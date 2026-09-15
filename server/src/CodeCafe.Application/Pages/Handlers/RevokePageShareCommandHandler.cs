using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Commands;
namespace CodeCafe.Application.Pages.Handlers;

public sealed class RevokePageShareCommandHandler : ICommandHandler<RevokePageShareCommand, Result>
{
    public Task<Result> Handle(RevokePageShareCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
