using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Revisions.RestoreBlockRevision;

public sealed class RestoreBlockRevisionCommandHandler : ICommandHandler<RestoreBlockRevisionCommand, Result>
{
    public Task<Result> Handle(RestoreBlockRevisionCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
