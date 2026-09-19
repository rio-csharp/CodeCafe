using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Trash.EmptyTrash;

public sealed class EmptyTrashCommandHandler : ICommandHandler<EmptyTrashCommand, Result>
{
    public Task<Result> Handle(EmptyTrashCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
