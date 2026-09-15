using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Commands;
using CodeCafe.Application.Pages.Models;
namespace CodeCafe.Application.Pages.Handlers;

public sealed class MovePageCommandHandler : ICommandHandler<MovePageCommand, Result<PageDetailsDto>>
{
    public Task<Result<PageDetailsDto>> Handle(MovePageCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
