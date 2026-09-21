using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Shared;
namespace CodeCafe.Application.Pages.MovePage;

public sealed class MovePageCommandHandler : ICommandHandler<MovePageCommand, Result<PageDetailsDto>>
{
    public Task<Result<PageDetailsDto>> Handle(MovePageCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
