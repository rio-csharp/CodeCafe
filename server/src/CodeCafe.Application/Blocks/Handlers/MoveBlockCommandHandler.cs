using CodeCafe.Application.Blocks.Commands;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Blocks.Handlers;

public sealed class MoveBlockCommandHandler : ICommandHandler<MoveBlockCommand, Result>
{
    public Task<Result> Handle(MoveBlockCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
