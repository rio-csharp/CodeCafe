using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Trash.Commands;

namespace CodeCafe.Application.Trash.Handlers;

public sealed class EmptyTrashCommandHandler : ICommandHandler<EmptyTrashCommand, Result>
{
    public Task<Result> Handle(EmptyTrashCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
