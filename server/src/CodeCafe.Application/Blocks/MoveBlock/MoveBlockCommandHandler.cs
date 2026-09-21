using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Blocks.MoveBlock;

public sealed class MoveBlockCommandHandler : ICommandHandler<MoveBlockCommand, Result>
{
    public Task<Result> Handle(MoveBlockCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
