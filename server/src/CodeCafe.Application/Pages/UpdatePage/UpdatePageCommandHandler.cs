using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.UpdatePage;

public sealed class UpdatePageCommandHandler : ICommandHandler<UpdatePageCommand, Result<PageDetailsDto>>
{
    public Task<Result<PageDetailsDto>> Handle(UpdatePageCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
