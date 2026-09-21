using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Blocks.DeleteBlock;

public sealed class DeleteBlockCommandHandler : ICommandHandler<DeleteBlockCommand, Result>
{
    public Task<Result> Handle(DeleteBlockCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
