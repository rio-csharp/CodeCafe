using CodeCafe.Application.Blocks.Shared;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Blocks.UpdateBlock;

public sealed class UpdateBlockCommandHandler : ICommandHandler<UpdateBlockCommand, Result<BlockDto>>
{
    public Task<Result<BlockDto>> Handle(UpdateBlockCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
