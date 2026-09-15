using CodeCafe.Application.Blocks.Commands;
using CodeCafe.Application.Blocks.Models;
using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
namespace CodeCafe.Application.Blocks.Handlers;

public sealed class UpdateBlockCommandHandler : ICommandHandler<UpdateBlockCommand, Result<BlockDto>>
{
    public Task<Result<BlockDto>> Handle(UpdateBlockCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
