using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Pages.RevokePageShare;

public sealed class RevokePageShareCommandHandler : ICommandHandler<RevokePageShareCommand, Result>
{
    public Task<Result> Handle(RevokePageShareCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
