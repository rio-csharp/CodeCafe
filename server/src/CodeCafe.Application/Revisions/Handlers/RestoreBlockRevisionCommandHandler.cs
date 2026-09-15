using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Revisions.Commands;
namespace CodeCafe.Application.Revisions.Handlers;

public sealed class RestoreBlockRevisionCommandHandler : ICommandHandler<RestoreBlockRevisionCommand, Result>
{
    public Task<Result> Handle(RestoreBlockRevisionCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
