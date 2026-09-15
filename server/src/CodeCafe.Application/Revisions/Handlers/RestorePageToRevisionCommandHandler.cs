using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Revisions.Commands;
namespace CodeCafe.Application.Revisions.Handlers;

public sealed class RestorePageToRevisionCommandHandler : ICommandHandler<RestorePageToRevisionCommand, Result>
{
    public Task<Result> Handle(RestorePageToRevisionCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
