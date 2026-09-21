using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Revisions.RestorePageToRevision;

public sealed class RestorePageToRevisionCommandHandler : ICommandHandler<RestorePageToRevisionCommand, Result>
{
    public Task<Result> Handle(RestorePageToRevisionCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
